using ApplicationCore.DTOs.Request;
using ApplicationCore.DTOs.Response;
using ApplicationCore.Services.Interface;
using ApplicationCore.Utilities;
using AutoMapper;
using Infrastructure.Accounts;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Services.Repository
{
    public class AuthenRepository : IAuthenRepository
    {
        private readonly AppDbContext _context;
        private readonly UserManager<UserReg> _userManager;//to create user
        private readonly SignInManager<UserReg> _signInManager;//  to signin user
        private readonly RoleManager<UserRoles> _roleManager;
        private readonly IMapper _Mapper;

        private readonly string key;
        private readonly int TokenExpirationPeriod;
        private readonly SecreteKeys _secretKey;
        public AuthenRepository(AppDbContext context, UserManager<UserReg> userManager,
           SignInManager<UserReg> signInManager, RoleManager<UserRoles> roleManager, IMapper Mapper,
           IOptions<SecreteKeys> secretKey
            )
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _Mapper = Mapper;
            _secretKey = secretKey.Value;
            this.key = _secretKey.Secret.ToString();
            TokenExpirationPeriod = _secretKey.TokenExpirationPeriod;
        }

        public async Task<LoginResponse> Authenticate(LoginDto LoginDetails)
        {
            var existedUser = await _userManager.FindByEmailAsync(LoginDetails.Email);
            if (existedUser != null)
            {
                try
                {
                    var isCorect = await _userManager.CheckPasswordAsync(existedUser, LoginDetails.Password);
                    if (isCorect)
                    {
                        var claim = await GetUserClaimRoles(existedUser);

                        var tokenHadler = new JwtSecurityTokenHandler();
                        var tokenKey = Encoding.ASCII.GetBytes(key);

                        var tokenDescriptor = new SecurityTokenDescriptor
                        {
                            Subject = new System.Security.Claims.ClaimsIdentity(claim),
                            Expires = DateTime.UtcNow.AddMinutes(TokenExpirationPeriod),
                            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)

                        };
                        var token = tokenHadler.CreateToken(tokenDescriptor);
                        var JwtOken = tokenHadler.WriteToken(token);

                        return new LoginResponse()
                        {
                            Token = JwtOken,
                            Success = true,
                            RefreshToken = "",
                            Message = "Login Success"

                        };
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {

                    throw;
                }
               
            }
            else
            {
                return null;

            }

        }

        private async  Task <IList<Claim>> GetUserClaimRoles(UserReg user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Name,user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
            };

            //Get the claims we have assigned the user
            var userClaims = await _userManager.GetClaimsAsync(user);//.ge
            claims.AddRange(userClaims);

            //get the user roll
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var userRole in userRoles)
            {
                //reconfirm if role exist
                var role = await _roleManager.FindByNameAsync(userRole);

                if (role != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole));
                }
            }

            return claims;
        }

        public async Task<IEnumerable<UserRegistrationDto>> getAllUsers()
        {
            var users = await (from user in _userManager.Users
                               join userRoles in _context.UserRoles on user.Id equals userRoles.UserId
                               join role in _roleManager.Roles on userRoles.RoleId equals role.Id
                               select new UserRegistrationDto
                               {
                                   UserId = user.Id,
                                   FirstName = user.FirstName,
                                   LastName = user.LastName,
                                   Email = user.Email,
                                   PhoneNumber = user.PhoneNumber,
                                   RoleName = _roleManager.Roles.Where(x => x.Id == userRoles.RoleId).Select(x => x.Name).ToList()

                               }

                                  ).ToListAsync();
          
            return (users);
        }

        public async Task<UserRegistrationResponse> RegisterUser(UserRegistrationDto users)
        {
            var newuser = _Mapper.Map<UserReg>(users);
            newuser.NormalizedUserName = users.UserName;
            newuser.NormalizedEmail = users.Email;
            var RS = new UserRegistrationResponse();

            var existed = await _userManager.FindByEmailAsync(users.Email);
            if(existed == null)
            {
                var isCreated = await _userManager.CreateAsync(newuser, users.Password);
                if(isCreated.Succeeded)
                {
                    var roleCheck = await _roleManager.RoleExistsAsync(newuser.UserRole);

                    var role = new UserRoles();
                    role.Name = newuser.UserRole;

                    if(!roleCheck)
                    {
                        await _roleManager.CreateAsync(role);
                    }

                    await _userManager.AddToRoleAsync(newuser, newuser.UserRole);

                    RS.Message = "User created successfully";
                    RS.Success = true;
                }
                else
                {
                    RS.Errors = isCreated.Errors.Select(x => x.Description).ToList();
                    RS.Success = false;
                }
            }
            else
            {
                RS.Errors = new List<string>()
                    {
                        "User already Exist"
                    };
                RS.Message = "User already in use";
                RS.Success = false;
            }



            return RS;
        }
    }
}
