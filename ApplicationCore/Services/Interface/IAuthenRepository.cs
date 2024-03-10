using ApplicationCore.DTOs.Request;
using ApplicationCore.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Services.Interface
{
    public interface IAuthenRepository
    {
        Task<UserRegistrationResponse> RegisterUser(UserRegistrationDto users);
        Task<IEnumerable<UserRegistrationDto>> getAllUsers();
        Task<LoginResponse> Authenticate(LoginDto LoginDetails);


    }
}
