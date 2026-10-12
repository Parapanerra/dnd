using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NUnit.Framework;

public class TaruckDialogTextTests
{
    private sealed class LanguageHolder
    {
        public object Value;
    }

    private static readonly Type FormatterType = Type.GetType("TaruckDialogText, Assembly-CSharp");
    private static readonly Type LanguageType = Type.GetType("AppLanguage, Assembly-CSharp");
    private static readonly Type FileType = typeof(TaruckFileType);
    private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    private static object Create(LanguageHolder holder)
    {
        Assert.NotNull(FormatterType);
        Assert.NotNull(LanguageType);
        Type delegateType = typeof(Func<>).MakeGenericType(LanguageType);
        Expression value = Expression.Field(Expression.Constant(holder), nameof(LanguageHolder.Value));
        Delegate language = Expression.Lambda(delegateType, Expression.Convert(value, LanguageType)).Compile();
        return Activator.CreateInstance(FormatterType, InstanceFlags, null, new object[] { language }, null);
    }

    private static string Call(object formatter, string method, params object[] arguments)
    {
        MethodInfo info = FormatterType.GetMethod(method, InstanceFlags);
        Assert.NotNull(info);
        return (string)info.Invoke(formatter, arguments);
    }

    [Test]
    public void ExistingFormatterUsesCurrentLanguageForEachDialog()
    {
        var holder = new LanguageHolder { Value = Enum.Parse(LanguageType, "Ukrainian") };
        object formatter = Create(holder);

        Assert.AreEqual("Закрити", Call(formatter, "Text", "Закрити", "Close"));
        holder.Value = Enum.Parse(LanguageType, "English");
        Assert.AreEqual("Close", Call(formatter, "Text", "Закрити", "Close"));
    }

    [Test]
    public void WrongFileTypeNamesBothFormatsInSelectedLanguage()
    {
        Assert.NotNull(FileType);
        byte[] file = new byte[11];
        Encoding.ASCII.GetBytes("TARUCKPK").CopyTo(file, 0);
        file[10] = Convert.ToByte(Enum.Parse(FileType, "CharacterExport"));
        var holder = new LanguageHolder { Value = Enum.Parse(LanguageType, "English") };
        object formatter = Create(holder);

        string message = Call(formatter, "ImportError",
            new InvalidDataException("Wrong file type"), file, Enum.Parse(FileType, "FullSaveExport"));

        StringAssert.Contains("one character (.tchar)", message);
        StringAssert.Contains("all characters (.tall)", message);
    }
}