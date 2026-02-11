using Microsoft.AspNetCore.Authorization;

namespace Play.Catalog.Service.Policies
{
    public class CatalogReadOrAdminRequirement : IAuthorizationRequirement {
    
    }

    public class CatalogReadOrAdminHandler : AuthorizationHandler<CatalogReadOrAdminRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CatalogReadOrAdminRequirement requirement)
        {
            var hasScope = context.User.HasClaim("scope", "catalog.readaccess");
            var isAdmin = context.User.IsInRole("Admin");

            if (hasScope || isAdmin)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
