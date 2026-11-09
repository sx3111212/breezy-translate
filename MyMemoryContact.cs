using System.Net.Mail;

namespace EnglishHelper;

static class MyMemoryContact
{
    public static string Validate(string? value)
    {
        var email = (value ?? "").Trim();
        if (email.Length == 0) return "";
        if (email.Length > 254 || email.Any(char.IsWhiteSpace) || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
            throw new Exception(UiText.T("emailInvalid"));
        return email;
    }
}
