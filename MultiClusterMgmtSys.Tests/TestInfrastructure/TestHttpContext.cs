using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;

namespace MultiClusterMgmtSys.Tests.TestInfrastructure;

public static class TestHttpContext
{
    public static Mock<IHttpContextAccessor> Anonymous()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns((HttpContext?)null);
        return accessor;
    }

    public static Mock<IHttpContextAccessor> For(string userName, params string[] roles)
        => ForIdentity(userName, null, roles);

    public static Mock<IHttpContextAccessor> ForIdentity(string userName, int? userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, userName) };
        if (userId is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var identity = new ClaimsIdentity(claims, "Test");

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity: identity)
        };

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);
        return accessor;
    }
}
