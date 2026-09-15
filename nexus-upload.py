#!/usr/bin/env python3
"""Uploads a mod's release zip to Nexus Mods as a new version of an existing file.

Nexus Mods' upload API (v3) cannot create mod pages: the page and its first file are made on the site by hand,
once. After that this script adds every new version. The IDs live in <Mod>/thunderstore/nexus.json:

    {"mod_id": 1234, "file_id": 5678}

mod_id is the number in the page URL (nexusmods.com/valheim/mods/1234); file_id is found with --list-files.

    python nexus-upload.py --mod FeastMaster --list-files      show the files of the mod page, to pick file_id
    python nexus-upload.py --mod FeastMaster                   upload thunderstore/<Mod>-<version>.zip

The version and zip come from thunderstore/manifest.json, the changelog text from the top section of CHANGELOG.md.
The API key comes from the NEXUS_API_KEY environment variable (personal key from nexusmods.com, Settings, API keys).
Never put the key in a file or the repository.
"""
import argparse
import json
import os
import re
import sys
import time
import urllib.error
import urllib.request

API_V1 = "https://api.nexusmods.com/v1"
API_V3 = "https://api.nexusmods.com/v3"
GAME = "valheim"


def api_key():
    key = os.environ.get("NEXUS_API_KEY", "").strip()
    if not key and os.name == "nt":
        import subprocess
        key = subprocess.run(
            ["powershell", "-NoProfile", "-Command", "[Environment]::GetEnvironmentVariable('NEXUS_API_KEY','User')"],
            capture_output=True, text=True).stdout.strip()
    if not key:
        sys.exit("NEXUS_API_KEY is not set. Create a personal API key on nexusmods.com (Settings, API keys) and run "
                 "setx NEXUS_API_KEY \"...\" in PowerShell.")
    return key


def request(method, url, key, body=None, content_type="application/json", raw=False):
    data = None
    headers = {"apikey": key, "Accept": "application/json"}
    if body is not None:
        data = body if raw else json.dumps(body).encode()
        headers["Content-Type"] = content_type
    req = urllib.request.Request(url, data=data, method=method, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            text = resp.read().decode()
            return resp.status, (json.loads(text) if text else {}), resp.headers
    except urllib.error.HTTPError as e:
        sys.exit(f"{method} {url} failed: {e.code} {e.read().decode()[:500]}")


def top_changelog(path):
    text = open(path, encoding="utf-8").read()
    m = re.search(r"^## (\d+\.\d+\.\d+)\s*\n(.*?)(?=^## |\Z)", text, re.S | re.M)
    return (m.group(1), m.group(2).strip()) if m else (None, "")


def list_files(mod_id, key):
    _, data, _ = request("GET", f"{API_V1}/games/{GAME}/mods/{mod_id}/files.json", key)
    for f in data.get("files", []):
        print(f"file_id {f['file_id']:>8}  v{f.get('version', '?'):<8} {f.get('category_name', '?'):<10} {f['name']}")


def upload(mod_dir, mod, ids, key, dry_run):
    manifest = json.load(open(os.path.join(mod_dir, "thunderstore", "manifest.json"), encoding="utf-8"))
    version = manifest["version_number"]
    zip_path = os.path.join(mod_dir, "thunderstore", f"{mod}-{version}.zip")
    if not os.path.exists(zip_path):
        sys.exit(f"{zip_path} does not exist; run pack.ps1 first")
    changelog_version, changelog = top_changelog(os.path.join(mod_dir, "CHANGELOG.md"))
    if changelog_version != version:
        sys.exit(f"CHANGELOG.md top section is {changelog_version}, manifest is {version}")
    size = os.path.getsize(zip_path)

    # The v3 API identifies mods by a global id and files by a "mod file" id that groups versions; both are
    # resolved from the per-game numbers the page shows (mod number in the URL, file_id from --list-files).
    _, mod_data, _ = request("GET", f"{API_V3}/games/{GAME}/mods/{ids['mod_id']}", key)
    v3_mod_id = mod_data["data"]["id"]
    _, ver_data, _ = request("GET", f"{API_V3}/games/{GAME}/mod-file-versions/{ids['file_id']}", key)
    v3_file_id = ver_data["data"]["file"]["id"]
    current = ver_data["data"]
    print(f"{mod} {version}: {os.path.basename(zip_path)} ({size} bytes) -> mod {ids['mod_id']} (v3 {v3_mod_id}), "
          f"file {ids['file_id']} '{current['name']}' {current['version']} (v3 file {v3_file_id})")
    if current["version"] == version:
        sys.exit(f"{mod} {version} is already the current version of that file on Nexus")
    if dry_run:
        print("dry run, nothing uploaded")
        return

    # 1. create the multipart upload
    _, created, _ = request("POST", f"{API_V3}/uploads/multipart", key,
                            {"filename": os.path.basename(zip_path), "size_bytes": str(size)})
    up = created["data"]
    upload_id, part_urls, part_size = up["id"], up["part_presigned_urls"], int(up["part_size_bytes"])

    # 2. upload the parts to the presigned URLs (no API key there)
    parts = []
    with open(zip_path, "rb") as f:
        for number, part_url in enumerate(part_urls, start=1):
            chunk = f.read(part_size)
            req = urllib.request.Request(part_url, data=chunk, method="PUT",
                                         headers={"Content-Type": "application/octet-stream"})
            with urllib.request.urlopen(req, timeout=600) as resp:
                parts.append((number, resp.headers.get("ETag")))
            print(f"  part {number}/{len(part_urls)} uploaded")

    # 3. complete the multipart upload
    xml = "<CompleteMultipartUpload>" + "".join(
        f"<Part><PartNumber>{n}</PartNumber><ETag>{etag}</ETag></Part>" for n, etag in parts) + "</CompleteMultipartUpload>"
    req = urllib.request.Request(up["complete_presigned_url"], data=xml.encode(), method="POST",
                                 headers={"Content-Type": "application/xml"})
    with urllib.request.urlopen(req, timeout=120):
        pass

    # 4. finalise and 5. wait until Nexus has processed it
    request("POST", f"{API_V3}/uploads/{upload_id}/finalise", key)
    delay = 2.0
    for _ in range(60):
        _, state, _ = request("GET", f"{API_V3}/uploads/{upload_id}", key)
        if state["data"]["state"] == "available":
            break
        time.sleep(delay)
        delay = min(delay * 1.5, 30)
    else:
        sys.exit("upload never became available")

    # 6. attach it to the mod file as a new version
    _, result, _ = request("POST", f"{API_V3}/mod-files/{v3_file_id}/versions", key, {
        "upload_id": upload_id,
        "name": f"{mod} {version}",
        "description": changelog[:1000] or f"{mod} {version}",
        "version": version,
        "file_category": "main",
        "archive_existing_file": True,
        "primary_mod_manager_download": True,
        "allow_mod_manager_download": True,
        "update_mod_version": True,
    })
    print(f"  version created: {result['data']['version']['id']}")

    # 7. changelog on the mod page
    if changelog:
        request("POST", f"{API_V3}/mods/{v3_mod_id}/changelogs", key, {"version": version, "changelog": changelog})
        print("  changelog added")
    print(f"done: https://www.nexusmods.com/{GAME}/mods/{ids['mod_id']}?tab=files")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--mod", required=True, help="mod folder name, e.g. FeastMaster")
    parser.add_argument("--list-files", action="store_true", help="list the mod page's files and exit")
    parser.add_argument("--dry-run", action="store_true", help="check everything, upload nothing")
    args = parser.parse_args()

    root = os.path.dirname(os.path.abspath(__file__))
    mod_dir = os.path.join(root, args.mod)
    ids_path = os.path.join(mod_dir, "thunderstore", "nexus.json")
    if not os.path.exists(ids_path):
        sys.exit(f"{ids_path} is missing. Create the mod page on nexusmods.com, upload the first file by hand, then "
                 "write {\"mod_id\": <page number>, \"file_id\": <from --list-files>} there.")
    ids = json.load(open(ids_path, encoding="utf-8"))
    key = api_key()
    if args.list_files:
        list_files(ids["mod_id"], key)
        return
    if "file_id" not in ids:
        sys.exit(f"{ids_path} has no file_id; run --list-files and add it")
    upload(mod_dir, args.mod, ids, key, args.dry_run)


if __name__ == "__main__":
    main()
