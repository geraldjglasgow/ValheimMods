#!/usr/bin/env python3
"""Posts a mod's wiki pages to its Thunderstore package wiki (the Wiki tab on the store page).

The pages live in <Mod>/thunderstore/wiki/, one Markdown file each, named NN-title.md so NN sets the order. The first
line of a file is "# Page Title": the store page title, stripped from the body. A link to another page of the same
wiki is written [text](wiki:Page Title) and rewritten here to that page's store URL.

    python wiki-upload.py --mod EliteCreaturesReborn --dry-run   check the pages and show what would change
    python wiki-upload.py --mod EliteCreaturesReborn             create and update the pages on the store
    python wiki-upload.py --all                                  every mod that has a wiki folder
    python wiki-upload.py --mod X --prune                        also delete store pages that have no file

Pages are matched to the store by title, so renaming a page's title creates a new page (and --prune removes the old).

Thunderstore lets only a team member's own login edit a wiki; the service account token tcli uses is refused. The
script authenticates with the browser session instead: log in on thunderstore.io, copy the value of the "sessionid"
cookie (browser dev tools, Application, Cookies) and run setx THUNDERSTORE_SESSION "..." in PowerShell, then open a
new shell. Never put it in a file, a script or the chat; it is your login. It expires when you log out.
"""
import argparse
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent
API = "https://thunderstore.io/api/experimental"
TEAM = "MilkyTeam"
COMMUNITY = "valheim"
MAX_CHARS = 100000
WIKI_LINK = re.compile(r"\]\(wiki:([^)]+)\)")
WRITE_GAP = 3                     # seconds after each write; Cloudflare answers 429 to bursts
RETRY_WAITS = (30, 60, 120, 240)  # seconds to wait before each retry of a rate-limited request


def session():
    value = os.environ.get("THUNDERSTORE_SESSION", "").strip()
    if not value and os.name == "nt":
        import subprocess
        value = subprocess.run(
            ["powershell", "-NoProfile", "-Command",
             "[Environment]::GetEnvironmentVariable('THUNDERSTORE_SESSION','User')"],
            capture_output=True, text=True).stdout.strip()
    if not value:
        sys.exit("THUNDERSTORE_SESSION is not set. Log in on thunderstore.io, copy the sessionid cookie and run "
                 "setx THUNDERSTORE_SESSION \"...\" in PowerShell (see the top of this script).")
    return value


def request(method, url, token=None, body=None):
    for wait in RETRY_WAITS:
        status, result = send(method, url, token, body)
        if status != 429:
            if method != "GET":
                time.sleep(WRITE_GAP)
            return status, result
        print(f"  rate limited, waiting {wait} s")
        time.sleep(wait)
    sys.exit(f"{method} {url} still rate limited after {len(RETRY_WAITS)} tries")


def send(method, url, token, body):
    if method == "GET":  # Cloudflare caches API reads, a 404 included, so each read asks for a fresh copy
        url += ("&" if "?" in url else "?") + f"_={time.time_ns()}"
    headers = {"Accept": "application/json", "User-Agent": "ValheimMods-wiki-upload"}
    if token:
        headers["Authorization"] = f"Session {token}"
    data = None
    if body is not None:
        data = json.dumps(body).encode()
        headers["Content-Type"] = "application/json"
    req = urllib.request.Request(url, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            text = resp.read().decode()
            return resp.status, (json.loads(text) if text else {})
    except urllib.error.HTTPError as e:
        if e.code == 404 and method == "GET":
            return 404, {}
        if e.code == 429:
            return 429, {}
        hint = " (is THUNDERSTORE_SESSION current and are you in MilkyTeam?)" if e.code in (401, 403) else ""
        sys.exit(f"{method} {url} failed: {e.code} {e.read().decode()[:500]}{hint}")


def read_pages(mod):
    folder = ROOT / mod / "thunderstore" / "wiki"
    pages = []
    for path in sorted(folder.glob("*.md")):
        lines = path.read_text(encoding="utf-8").splitlines()
        if not lines or not lines[0].startswith("# "):
            sys.exit(f"{path}: the first line must be '# Page Title'")
        body = "\n".join(lines[1:]).strip() + "\n"
        pages.append({"file": path.name, "title": lines[0][2:].strip(), "body": body})
    titles = [p["title"] for p in pages]
    for title in titles:
        if titles.count(title) > 1:
            sys.exit(f"{mod}: two pages are titled '{title}'")
    for page in pages:
        for target in WIKI_LINK.findall(page["body"]):
            if target.strip() not in titles:
                sys.exit(f"{mod}/{page['file']}: link to unknown page '{target}'")
    return pages


def store_pages(mod, token=None):
    status, wiki = request("GET", f"{API}/package/{TEAM}/{mod}/wiki/", token)
    return {} if status == 404 else {p["title"]: p for p in wiki.get("pages", [])}


def page_url(mod, slug):
    return f"https://thunderstore.io/c/{COMMUNITY}/p/{TEAM}/{mod}/wiki/{slug}/"


def resolve(body, mod, slugs):
    def link(match):
        slug = slugs.get(match.group(1).strip())
        return f"]({page_url(mod, slug)})" if slug else match.group(0)
    return WIKI_LINK.sub(link, body)


def store_content(page_id, token):
    return request("GET", f"{API}/wiki/page/{page_id}/", token)[1].get("markdown_content", "")


def upsert(mod, token, title, content, page_id=None):
    body = {"title": title, "markdown_content": content}
    if page_id:
        body["id"] = str(page_id)
    return request("POST", f"{API}/package/{TEAM}/{mod}/wiki/", token, body)[1]


def create_missing(mod, token, pages, existing):
    # A new page needs its slug before other pages can link to it, so it is created first and filled in pass two.
    for page in pages:
        if page["title"] not in existing:
            existing[page["title"]] = upsert(mod, token, page["title"], page["body"])
            print(f"  created  {page['title']}")


def publish(mod, prune, dry_run):
    pages = read_pages(mod)
    if not pages:
        print(f"{mod}: no pages in thunderstore/wiki/")
        return
    for page in pages:
        if len(page["body"]) > MAX_CHARS:
            sys.exit(f"{mod}/{page['file']}: {len(page['body'])} characters, the store takes {MAX_CHARS}")
    token = None if dry_run else session()
    existing = store_pages(mod, token)
    print(f"{mod}: {len(pages)} pages here, {len(existing)} on the store")
    stale = [t for t in existing if t not in {p["title"] for p in pages}]
    if dry_run:
        for page in pages:
            print(f"  {'update' if page['title'] in existing else 'create'}  {page['title']}  ({page['file']})")
        for title in stale:
            print(f"  {'delete' if prune else 'keep'}    {title}  (no file)")
        return
    create_missing(mod, token, pages, existing)
    slugs = {title: p["slug"] for title, p in existing.items()}
    for page in pages:
        content = resolve(page["body"], mod, slugs)
        entry = existing[page["title"]]
        if store_content(entry["id"], token).strip() != content.strip():
            upsert(mod, token, page["title"], content, entry["id"])
            print(f"  updated  {page['title']}")
    for title in stale:
        if prune:
            request("DELETE", f"{API}/package/{TEAM}/{mod}/wiki/", token, {"id": str(existing[title]["id"])})
            print(f"  deleted  {title}")
        else:
            print(f"  kept     {title}  (no file; --prune deletes it)")
    print(f"  https://thunderstore.io/c/{COMMUNITY}/p/{TEAM}/{mod}/wiki/")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--mod", help="the mod folder name, which is also the package name")
    group.add_argument("--all", action="store_true", help="every mod with a thunderstore/wiki folder")
    parser.add_argument("--dry-run", action="store_true", help="check the pages and show what would change")
    parser.add_argument("--prune", action="store_true", help="delete store pages that have no file")
    args = parser.parse_args()
    mods = [args.mod] if args.mod else sorted(p.parent.parent.name for p in ROOT.glob("*/thunderstore/wiki"))
    for mod in mods:
        if not (ROOT / mod / "thunderstore" / "wiki").is_dir():
            sys.exit(f"{mod}/thunderstore/wiki/ does not exist")
        publish(mod, args.prune, args.dry_run)


if __name__ == "__main__":
    main()
