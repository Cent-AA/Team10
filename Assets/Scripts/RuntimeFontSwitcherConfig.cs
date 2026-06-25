using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "Team10/Runtime Font Switcher Config")]
public sealed class RuntimeFontSwitcherConfig : ScriptableObject
{
    [SerializeField] private TMP_FontAsset tmpFontAsset;
    [SerializeField] private Font legacyFont;
    [SerializeField, Min(0.05f)] private float rescanInterval = 0.25f;

    public TMP_FontAsset TMPFontAsset => tmpFontAsset;
    public Font LegacyFont => legacyFont;
    public float RescanInterval => Mathf.Max(0.05f, rescanInterval);
}
