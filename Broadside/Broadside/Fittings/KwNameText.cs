using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The ship's name drawn on a name board (<see cref="KwNameBoard"/>) the way the game's sign draws its words: a copy of
    /// the sign's own canvas and TextMeshPro text (its font, material, colour and outline), taken from the game's
    /// <c>sign</c> prefab at run time, once on each face, a few millimetres off it and reading from outside it. The text is
    /// sized to the space inside the frame, shrinking a long name to fit on one line; rich text is off, so a name shows as
    /// typed. Only drawn: nothing where there is no screen, or without the game's sign.
    /// </summary>
    public static class KwNameText
    {
        private const float Off = 0.004f, Smallest = 0.03f;

        /// <summary>The name's text on both faces of a board placed at board (its own frame as the piece's); none on a server.</summary>
        public static List<TMP_Text> Add(Transform board)
        {
            List<TMP_Text> texts = new List<TMP_Text>();
            Sign? sign = KwShipBody.Drawn && ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("sign")?.GetComponent<Sign>() : null;
            if (sign == null || sign.m_textWidget == null)
                return texts;
            foreach (bool front in new[] { true, false })
            {
                if (Face(board, sign, front) is TMP_Text text)
                    texts.Add(text);
            }
            return texts;
        }

        /// <summary>Every text shown with a name.</summary>
        public static void Show(List<TMP_Text> texts, string name)
        {
            foreach (TMP_Text text in texts)
            {
                if (text != null)
                    text.text = name;
            }
        }

        // One face's copy of the sign's canvas: on the face, turned to read from outside it, the text fitted inside the frame.
        private static TMP_Text? Face(Transform board, Sign sign, bool front)
        {
            Canvas? canvas = sign.m_textWidget.GetComponentInParent<Canvas>(true);
            Transform source = canvas != null ? canvas.transform : sign.m_textWidget.transform;
            GameObject copy = Object.Instantiate(source.gameObject, board, false);
            copy.name = front ? "name_front" : "name_back";
            copy.transform.localScale = Relative(source.lossyScale, sign.transform.lossyScale);
            copy.transform.localPosition = KwMesh.ToUnity(new Vector3(front ? KwNameBoard.Face + Off : -Off, 0f, 0f));
            copy.transform.localRotation = Quaternion.LookRotation(front ? Vector3.back : Vector3.forward, Vector3.up);
            foreach (UnityEngine.UI.GraphicRaycaster raycaster in copy.GetComponentsInChildren<UnityEngine.UI.GraphicRaycaster>(true))
                Object.Destroy(raycaster);
            TMP_Text? text = copy.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
                Fit(copy.transform, text, board);
            return text;
        }

        // The text centred on the face, as wide and high as the space inside the frame, one line, shrinking to fit.
        private static void Fit(Transform canvas, TMP_Text text, Transform board)
        {
            text.transform.SetPositionAndRotation(canvas.position, canvas.rotation);
            float scale = Mathf.Max(1e-5f, text.transform.lossyScale.x / Mathf.Max(1e-5f, board.lossyScale.x));
            RectTransform rect = text.rectTransform;
            (rect.anchorMin, rect.anchorMax, rect.pivot) = (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rect.sizeDelta = new Vector2(KwNameBoard.TextWide, KwNameBoard.TextHigh) / scale;
            if (canvas is RectTransform space)
                space.sizeDelta = new Vector2(KwNameBoard.TextWide, KwNameBoard.TextHigh) / Mathf.Max(1e-5f, canvas.lossyScale.x / Mathf.Max(1e-5f, board.lossyScale.x));
            text.transform.SetPositionAndRotation(canvas.position, canvas.rotation);
            (text.enableAutoSizing, text.fontSizeMin, text.fontSizeMax) = (true, Smallest / scale, KwNameBoard.TextHigh * 0.9f / scale);
            (text.textWrappingMode, text.alignment, text.richText, text.text) = (TextWrappingModes.NoWrap, TextAlignmentOptions.Center, false, "");
        }

        private static Vector3 Relative(Vector3 scale, Vector3 of) =>
            new Vector3(scale.x / Mathf.Max(1e-5f, of.x), scale.y / Mathf.Max(1e-5f, of.y), scale.z / Mathf.Max(1e-5f, of.z));
    }
}
