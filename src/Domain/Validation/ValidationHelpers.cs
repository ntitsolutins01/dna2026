using System.Text.RegularExpressions;

namespace DnaBrasilApi.Domain.Validation;

public static class ValidationHelpers
{
    public static bool CpfValido(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return false;
        var digits = Regex.Replace(cpf, "[^0-9]", "");
        if (digits.Length != 11) return false;
        if (new string(digits[0], 11) == digits) return false;

       static int Calc(string baseDigits)
        {
            int soma = 0;
            for (int i = 0; i < baseDigits.Length; i++)
                soma += (baseDigits[i] - '0') * (baseDigits.Length + 1 - i);
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        var dv1 = Calc(digits[..9]);
        if (dv1 != (digits[9] - '0')) return false;
        var dv2 = Calc(digits[..10]);
        if (dv2 != (digits[10] - '0')) return false;
        return true;
    }

    public static bool CelularValido(string? celular)
    {
        if (string.IsNullOrWhiteSpace(celular)) return false;
        var digits = Regex.Replace(celular, "[^0-9]", "");
        if (digits.Length != 11) return false;
        if (!int.TryParse(digits[..2], out var ddd) || ddd < 11 || ddd > 99) return false;
        if (digits[2] != '9') return false;
        if (new string(digits[0], 11) == digits) return false;
        return true;
    }

    public static bool TelefoneFixoValido(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone)) return true;
        var digits = Regex.Replace(telefone, "[^0-9]", "");
        if (digits.Length != 10) return false;
        if (!int.TryParse(digits[..2], out var ddd) || ddd < 11 || ddd > 99) return false;
        if (digits[2] == '9') return false;
        if (new string(digits[0], 10) == digits) return false;
        return true;
    }

    public static bool DataNascimentoValida(string? data)
    {
        if (string.IsNullOrWhiteSpace(data)) return false;
        if (!DateTime.TryParseExact(data, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            return false;
        if (dt > DateTime.Today) return false;
        if (dt < DateTime.Today.AddYears(-120)) return false;
        return true;
    }

    public static bool EmailValido(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        email = email.Trim();
        if (email.Length > 100) return false;
        var emailPattern = @"^[a-zA-Z0-9._%-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase);
    }
}
