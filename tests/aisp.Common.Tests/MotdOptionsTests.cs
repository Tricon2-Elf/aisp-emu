using aisp.Common.Config;
using aisp.Common.Localisation;
using Xunit;

namespace aisp.Common.Tests;

public class MotdOptionsTests
{
    [Fact]
    public void TryGetMessage_Disabled_ReturnsFalseEvenWhenMessageIsSet()
    {
        var options = new MotdOptions { Enabled = false, Message = "Welcome" };

        Assert.False(options.TryGetMessage(GameLanguage.English, out var message));
        Assert.Equal("", message);
    }

    [Fact]
    public void TryGetMessage_EmptyMessage_ReturnsFalse()
    {
        var options = new MotdOptions { Enabled = true, Message = "  " };

        Assert.False(options.TryGetMessage(GameLanguage.English, out _));
    }

    [Fact]
    public void TryGetMessage_UsesFallbackMessage()
    {
        var options = new MotdOptions { Enabled = true, Message = "  Hello world  " };

        Assert.True(options.TryGetMessage(GameLanguage.Japanese, out var message));
        Assert.Equal("Hello world", message);
    }

    [Fact]
    public void TryGetMessage_PrefersLanguageSpecificMessage()
    {
        var options = new MotdOptions
        {
            Enabled = true,
            Message = "fallback",
            Messages =
            {
                ["en"] = "Welcome",
                ["ja"] = "ようこそ",
                ["zh-CN"] = "欢迎",
            },
        };

        Assert.True(options.TryGetMessage(GameLanguage.English, out var english));
        Assert.Equal("Welcome", english);
        Assert.True(options.TryGetMessage(GameLanguage.Japanese, out var japanese));
        Assert.Equal("ようこそ", japanese);
        Assert.True(options.TryGetMessage(GameLanguage.ChineseSimplified, out var chinese));
        Assert.Equal("欢迎", chinese);
        Assert.True(options.TryGetMessage(GameLanguage.ChineseTraditional, out var traditional));
        Assert.Equal("fallback", traditional);
    }
}
