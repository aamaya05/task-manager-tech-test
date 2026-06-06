using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TaskManager.Domain.Entities;
using TaskManager.Domain.ValueObjects;
using TaskManager.Infrastructure.Auth;

namespace TaskManager.Infrastructure.Tests.Auth;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _jwtService;

    public JwtTokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-that-is-long-enough-for-hs256-signing",
                ["Jwt:Issuer"] = "taskmanager-api",
                ["Jwt:Audience"] = "taskmanager-client",
                ["Jwt:ExpiryHours"] = "1"
            })
            .Build();

        _jwtService = new JwtTokenService(config);
    }

    [Fact]
    public void JwtTokenService_GenerateToken_ReturnsThreeSegmentString()
    {
        var user = User.Create("alice", new Email("alice@example.com"), "hash");

        var token = _jwtService.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void JwtTokenService_GenerateToken_TokenContainsCorrectSubAndEmailClaims()
    {
        var user = User.Create("alice", new Email("alice@example.com"), "hash");
        var token = _jwtService.GenerateToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Subject.Should().Be(user.Id.ToString());
        jwtToken.Claims.First(c => c.Type == "email").Value.Should().Be("alice@example.com");
        jwtToken.Claims.First(c => c.Type == "username").Value.Should().Be("alice");
    }

    [Fact]
    public void JwtTokenService_GenerateToken_TokenExpiresApproximatelyOneHourFromNow()
    {
        var user = User.Create("alice", new Email("alice@example.com"), "hash");
        var token = _jwtService.GenerateToken(user);
        var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeCloseTo(
            DateTime.UtcNow.AddHours(1),
            precision: TimeSpan.FromSeconds(10));
    }
}
