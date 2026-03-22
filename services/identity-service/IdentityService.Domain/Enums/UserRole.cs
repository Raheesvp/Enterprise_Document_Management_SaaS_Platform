
namespace IdentityService.Domain.Enums;


//admin-full access , manage tenant, users
//manager - approve doucuments, manage workflows
//viewer - readonly access
public enum UserRole
{
    Admin =1,
    Manager =2,
    Viewer =3
}
