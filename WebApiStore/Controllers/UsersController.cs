using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApiStore.DTOs.User;
using WebApiStore.Services;

namespace WebApiStore.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly IConfiguration configuration;
        private readonly SignInManager<IdentityUser> signInManager;
        private readonly HashService hashService;
        private readonly IDataProtector dataProtector;

        public UsersController(
            UserManager<IdentityUser> userManager,
            IConfiguration configuration,
            SignInManager<IdentityUser> signInManager,
            IDataProtectionProvider dataProtectionProvider,
            HashService hashService
        )
        {
            this.userManager = userManager;
            this.configuration = configuration;
            this.signInManager = signInManager;
            this.hashService = hashService;
            // string de propósito
            dataProtector = dataProtectionProvider.CreateProtector("ASDdscgrnSMbfk>X%fgs-+");
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthenticationResponse>> Register([FromForm] RegisterUser registerUser)
        {
            var usuario = new IdentityUser
            {
                UserName = registerUser.UserName,
                Email = registerUser.Email
            };
            var result = await userManager.CreateAsync(usuario, registerUser.Password);

            if (result.Succeeded)
            {
                // se retorna el JWT: Estandar especifica el formato del
                // Token que hay que devolverle a los usuarios para que puedan usar el Token para authenticación
                UserCredentials userCredentials = new UserCredentials()
                {
                    UserName = registerUser.UserName,
                    Password = registerUser.Password
                };
                return await BuildToken(userCredentials);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthenticationResponse>> Login([FromForm] UserCredentials userCredentials)
        {
            var result = await signInManager.PasswordSignInAsync(userCredentials.UserName,
                userCredentials.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return await BuildToken(userCredentials);
            }
            else
            {
                return BadRequest("Login incorrecto");
            }
        }

        [HttpGet("renewToken")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<AuthenticationResponse>> Renew()
        {
            var userNameClaim = HttpContext.User.Claims.Where(claim => claim.Type == "userName").FirstOrDefault();
            var userName = userNameClaim.Value;
            var userCredentials = new UserCredentials()
            {
                UserName = userName
            };
            return await BuildToken(userCredentials);
        }

        // Endpoints para hacer administradores y eliminarlos
        [HttpPost("makeAdmin")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]
        public async Task<ActionResult> MakeAdmin([FromForm] EditAdmin editAdmin)
        {
            var user = await userManager.FindByNameAsync(editAdmin.UserName);
            await userManager.AddClaimAsync(user, new Claim("admin", "true"));
            return NoContent();
        }

        [HttpPost("removeAdmin")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]
        public async Task<ActionResult> RemoveAdmin([FromForm] EditAdmin editAdmin)
        {
            var user = await userManager.FindByNameAsync(editAdmin.UserName);
            await userManager.RemoveClaimAsync(user, new Claim("admin", "true"));
            return NoContent();
        }

        [HttpGet("hash/{plainText}")]
        public ActionResult ToHash(string plainText)
        {
            var hash1 = hashService.Hash(plainText);
            var hash2 = hashService.Hash(plainText);
            return Ok(new
            {
                PlainText = plainText,
                Hash1 = hash1,
                Hash2 = hash2
            });
        }

        [HttpGet("Encrypt/{plainText}")]
        public ActionResult Encrypt(string plainText)
        {
            var cipherText = dataProtector.Protect(plainText);
            var decryptedText = dataProtector.Unprotect(cipherText);

            return Ok(new
            {
                plainText = plainText,
                cipherText = cipherText,
                decryptedText = decryptedText
            });
        }

        private async Task<AuthenticationResponse> BuildToken(UserCredentials userCredentials)
        {
            // Estos claims se añaden al token y se podrán leer
            // puenden ser leidos también por el cliente, por eso no se debe agregar datos sensibles en claims
            var claims = new List<Claim>()
            {
                new Claim("userName", userCredentials.UserName),
            };
            // Obtener claims del usuario autenticado
            var user = await userManager.FindByNameAsync(userCredentials.UserName);
            var claimsDB = await userManager.GetClaimsAsync(user);
            // Se agrega
            claims.AddRange(claimsDB);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["secretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiration = DateTime.UtcNow.AddYears(1);

            var securityToken = new JwtSecurityToken(issuer: null, audience: null,
                claims: claims, expires: expiration, signingCredentials: creds);
            return new AuthenticationResponse()
            {
                Token = new JwtSecurityTokenHandler().WriteToken(securityToken),
                Expiration = expiration
            };
        }        
    }
}