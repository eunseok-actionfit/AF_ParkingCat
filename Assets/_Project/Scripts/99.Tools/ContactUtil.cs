
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class ContactUtil
{
    #region Contant

    public const string Privacy_Policy_EN = "https://actionfit.co.kr/privacy-policy/";
    public const string Privacy_Policy_KR = "https://sites.google.com/view/actionfit-termsofservice-kr";

    #endregion

    #region Public API

    public static void Contact()
    {
        StringBuilder bodyBuilder = GetInformation();
        string toMail = "actionfitspt@gmail.com";
        string subject = EscapeURL("[Parking Cat]" + GetSubject());
        string body = EscapeURL(bodyBuilder + "\n\n");
        bodyBuilder.AppendLine("\nDevice Information:");
        bodyBuilder.AppendLine("Device Model: " + SystemInfo.deviceModel);
        bodyBuilder.AppendLine("Operating System: " + SystemInfo.operatingSystem);
        bodyBuilder.AppendLine("Graphics Device Name: " + SystemInfo.graphicsDeviceName);
        //bodyBuilder.AppendLine("Status Code: " + ctr.CurrentWave.ToString("D5") + "_000_" + ctr.CurrentOrderIdx.ToString("D5"));
        // if (ExceptionLogManager.GetExceptionLogs().Length > 0)
        // {
        //     bodyBuilder.AppendLine("============ Do Not Delete This Log ===========\n");
        //     bodyBuilder.AppendLine("Exception Logs:\n");
        //     bodyBuilder.AppendLine(ExceptionLogManager.GetExceptionLogs());
        // }
        OpenMailClient(toMail, subject, body);
    }

    public static void Contact(string feedback)
    {
        string toMail = "actionfitspt@gmail.com";
        string subject = EscapeURL(GetSubject());
        string body = EscapeURL(feedback + "\n\n" + GetInformation() + "\n\n");
        OpenMailClient(toMail, subject, body);
    }

    #endregion

    #region Internal

    private static StringBuilder GetInformation()
    {
        StringBuilder bodyBuilder = new StringBuilder();
        bodyBuilder.AppendLine($"Version_Platform: {Application.version}_{UnityEngine.Device.Application.platform}");
// #if UNITY_ANDROID
        //bodyBuilder.AppendLine($"ID: {Social.localUser.id}\n");
// #elif UNITY_IOS
//      //bodyBuilder.AppendLine($"ID: {Social.localUser.userName}\n");
// #endif
        bodyBuilder.AppendLine("Device Model: " + SystemInfo.deviceModel);
        bodyBuilder.AppendLine("Operating System: " + SystemInfo.operatingSystem);
        bodyBuilder.AppendLine("Graphics Device Name: " + SystemInfo.graphicsDeviceName);
        return bodyBuilder;
    }

    private static string EscapeURL(string url)
    {
        return UnityWebRequest.EscapeURL(url).Replace("+", "%20");
    }

    private static void OpenMailClient(string toMail, string subject, string body)
    {
        string mailtoUrl = $"mailto:{toMail}?subject={subject}&body={body}";
        Application.OpenURL(mailtoUrl);
    }

    private static string GetSubject()
    {
        return Application.systemLanguage switch
        {
            SystemLanguage.Korean => "문의하기",
            SystemLanguage.English => "Inquiry",
            SystemLanguage.ChineseTraditional => "咨询",
            SystemLanguage.ChineseSimplified => "咨询",
            SystemLanguage.Japanese => "お問い合わせ",
            _ => "Inquiry"
        };
    }

    #endregion
}