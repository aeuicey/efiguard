using EfiGuardUI.Models;

namespace EfiGuardUI.Views;

public class StatusCardDef
{
    public string Key { get; }
    public string TitleZh { get; }
    public string DescZh { get; }
    public string TitleEn { get; }
    public string DescEn { get; }
    public Func<SecurityStatus, object?> Getter { get; }

    public StatusCardDef(string key, string titleZh, string descZh, string titleEn, string descEn, Func<SecurityStatus, object?> getter)
    {
        Key = key;
        TitleZh = titleZh;
        DescZh = descZh;
        TitleEn = titleEn;
        DescEn = descEn;
        Getter = getter;
    }
}
