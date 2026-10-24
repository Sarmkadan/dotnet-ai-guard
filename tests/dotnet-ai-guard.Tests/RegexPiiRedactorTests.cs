namespace AiGuard.Tests;

public class RegexPiiRedactorTests
{
    private static RegexPiiRedactor CreateRedactor(PiiCategory categories = PiiCategory.All)
    {
        var monitor = new TestOptionsMonitor<AiGuardOptions>(new AiGuardOptions { RedactCategories = categories });
        return new RegexPiiRedactor(monitor);
    }

    [Fact]
    public async Task RedactAsync_EmailAndPhone_RedactedTextUsesPlaceholders()
    {
        var result = await CreateRedactor().RedactAsync("Contact john.doe@example.com or +49 178 5729777");

        Assert.Equal("Contact [EMAIL_1] or [PHONE_1]", result.RedactedText);
    }

    [Fact]
    public async Task RedactAsync_EmailAndPhone_ReportsTwoEntitiesInDocumentOrder()
    {
        var result = await CreateRedactor().RedactAsync("Contact john.doe@example.com or +49 178 5729777");

        Assert.Collection(result.Entities,
            e => Assert.Equal(PiiCategory.Email, e.Category),
            e => Assert.Equal(PiiCategory.Phone, e.Category));
    }

    [Fact]
    public async Task RedactAsync_EmailAndPhone_SpansPointAtOriginalSubstrings()
    {
        const string text = "Contact john.doe@example.com or +49 178 5729777";

        var result = await CreateRedactor().RedactAsync(text);

        Assert.Collection(result.Entities,
            e => Assert.Equal("john.doe@example.com", text.Substring(e.Span.Start, e.Span.Length)),
            e => Assert.Equal("+49 178 5729777", text.Substring(e.Span.Start, e.Span.Length)));
    }

    [Fact]
    public async Task RedactAsync_TwoEmails_SecondPlaceholderIsEmail2()
    {
        var result = await CreateRedactor().RedactAsync("a@example.com and b@example.org");

        Assert.Equal("[EMAIL_1] and [EMAIL_2]", result.RedactedText);
    }

    [Fact]
    public async Task RedactAsync_ValidLuhnCard_RedactedToCreditCardPlaceholder()
    {
        var result = await CreateRedactor().RedactAsync("Card 4111 1111 1111 1111 on file");

        Assert.Equal("Card [CREDITCARD_1] on file", result.RedactedText);
    }

    [Fact]
    public async Task RedactAsync_CardWithBadLuhn_LeftUntouched()
    {
        const string text = "Card 4111 1111 1111 1112 on file";

        var result = await CreateRedactor().RedactAsync(text);

        Assert.Equal(text, result.RedactedText);
    }

    [Fact]
    public async Task RedactAsync_EmailOnlyCategory_PhoneLeftIntact()
    {
        var result = await CreateRedactor(PiiCategory.Email)
            .RedactAsync("Contact john.doe@example.com or +49 178 5729777");

        Assert.Equal("Contact [EMAIL_1] or +49 178 5729777", result.RedactedText);
    }

    [Fact]
    public async Task RedactAsync_NoneCategory_ReturnsOriginalText()
    {
        const string text = "Contact john.doe@example.com or +49 178 5729777";

        var result = await CreateRedactor(PiiCategory.None).RedactAsync(text);

        Assert.Equal(text, result.RedactedText);
    }

    [Fact]
    public async Task RedactAsync_NoneCategory_HasRedactionsFalse()
    {
        var result = await CreateRedactor(PiiCategory.None)
            .RedactAsync("Contact john.doe@example.com or +49 178 5729777");

        Assert.False(result.HasRedactions);
    }

    [Fact]
    public async Task RedactAsync_Iban_RedactedToIbanPlaceholder()
    {
        var result = await CreateRedactor().RedactAsync("DE89370400440532013000");

        Assert.Equal("[IBAN_1]", result.RedactedText);
    }

    [Fact]
    public async Task RedactAsync_Ipv4Address_RedactedToIpAddressPlaceholder()
    {
        var result = await CreateRedactor().RedactAsync("Host 192.168.1.10 is up");

        Assert.Equal("Host [IPADDRESS_1] is up", result.RedactedText);
    }

    [Fact]
    public async Task RedactAsync_OutOfRangeIpv4_LeftUntouched()
    {
        const string text = "Host 999.1.1.1 is up";

        var result = await CreateRedactor().RedactAsync(text);

        Assert.Equal(text, result.RedactedText);
    }
}
