using System.Text;
using System.Text.RegularExpressions;

namespace ChinhNha.Application.Helpers;

public static class SlugHelper
{
    public static string GenerateSlug(string phrase)
    {
        string str = RemovePresign(phrase).ToLower();
        
        // invalid chars           
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        // convert multiple spaces into one space   
        str = Regex.Replace(str, @"\s+", " ").Trim();
        // cut and trim 
        str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
        str = Regex.Replace(str, @"\s", "-"); // hyphens   
        
        return str;
    }

    private static string RemovePresign(string text)
    {
        Regex regex = new Regex(@"\p{IsCombiningDiacriticalMarks}+");
        string strFormD = text.Normalize(NormalizationForm.FormD);
        string newString = regex.Replace(strFormD, string.Empty).Replace('\u0111', 'd').Replace('\u0110', 'D');
        
        // Handle specific Vietnamese chars correctly (though FormD handles most)
        newString = Regex.Replace(newString, "[áàảãạâấầẩẫậăắằẳẵặ]", "a");
        newString = Regex.Replace(newString, "[éèẻẽẹêếềểễệ]", "e");
        newString = Regex.Replace(newString, "[íìỉĩị]", "i");
        newString = Regex.Replace(newString, "[óòỏõọôốồổỗộơớờởỡợ]", "o");
        newString = Regex.Replace(newString, "[úùủũụưứừửữự]", "u");
        newString = Regex.Replace(newString, "[ýỳỷỹỵ]", "y");
        newString = Regex.Replace(newString, "[đ]", "d");

        return newString.Normalize(NormalizationForm.FormC);
    }
}
