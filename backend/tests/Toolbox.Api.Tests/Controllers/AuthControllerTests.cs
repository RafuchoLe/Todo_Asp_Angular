using FluentAssertions;                                                                                                  
using Microsoft.AspNetCore.Http;                                                                                         
using Microsoft.AspNetCore.Mvc;                                                                                          
using Microsoft.Extensions.Configuration;                                                                                
using Moq;                                                                                                               
using System.Security.Claims;                                                                                            
using Toolbox.API.Controllers;                                                                                           
using Toolbox.API.DTOs.Auth;                                                                                             
using Toolbox.Core.Entities;                                                                                             
using Toolbox.Core.Interfaces;                                                                                           
using Toolbox.Infrastructure.Identity;                                                                                   
                                                                                                                        
 namespace Toolbox.API.Tests.Controllers;                                                                                 
                                                                                                                          
 public class AuthControllerTests                                                                                         
 {                                                                                                                        
     private readonly Mock<IAuthService> _authServiceMock;                                                                
     private readonly JwtTokenGenerator _tokenGenerator;                                                                  
     private readonly AuthController _controller;                                                                         
                                                                                                                          
     public AuthControllerTests()                                                                                         
     {                                                                                                                    
         _authServiceMock = new Mock<IAuthService>();                                                                     
                                                                                                                          
         // Crear IConfiguration en memoria con valores de prueba para JWT                                                
         var configuration = new ConfigurationBuilder()                                                                   
             .AddInMemoryCollection(new Dictionary<string, string?>                                                       
             {                                                                                                            
                 {"JwtSettings:SecretKey", "ClaveSecretaDePruebaQueTieneAlMenos32Caracteres!"},                           
                 {"JwtSettings:Issuer", "TestIssuer"},                                                                    
                 {"JwtSettings:Audience", "TestAudience"},                                                                
                 {"JwtSettings:ExpirationMinutes", "60"}                                                                  
             })                                                                                                           
             .Build();                                                                                                    
                                                                                                                          
         _tokenGenerator = new JwtTokenGenerator(configuration);                                                          
         _controller = new AuthController(_authServiceMock.Object, _tokenGenerator);                                      
     }                                                                                                                    
                                                                                                                          
     // --- REGISTER ---                                                                                                  
                                                                                                                          
     [Fact]                                                                                                               
     public async Task Register_Should_Return_Ok_With_AuthResponse_On_Success()                                           
     {                                                                                                                    
         // Arrange — configurar el mock para simular registro exitoso                                                    
         var request = new RegisterRequest                                                                                
         {                                                                                                                
             Email = "test@example.com",                                                                                  
             Password = "password123",                                                                                    
             FirstName = "John",                                                                                          
             LastName = "Doe"                                                                                             
         };                                                                                                               
         var user = new User                                                                                              
         {                                                                                                                
             Email = request.Email,                                                                                       
             FirstName = request.FirstName,                                                                               
             LastName = request.LastName,                                                                                 
             PasswordHash = "hashedpassword"                                                                              
         };                                                                                                               
                                                                                                                          
         _authServiceMock                                                                                                 
             .Setup(s => s.RegisterAsync(request.Email, request.Password, request.FirstName, request.LastName))           
             .ReturnsAsync((true, user, (string?)null));                                                                  
                                                                                                                          
         // Act                                                                                                           
         var result = await _controller.Register(request);                                                                
                                                                                                                          
         // Assert                                                                                                        
         var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;                                        
         var response = okResult.Value.Should().BeOfType<AuthResponse>().Subject;                                         
         response.Token.Should().NotBeNullOrEmpty();                                                                      
         response.User.Email.Should().Be(request.Email);                                                                  
         response.User.Id.Should().Be(user.Id);                                                                           
     }                                                                                                                    
                                                                                                                          
     [Fact]                                                                                                               
     public async Task Register_Should_Return_BadRequest_When_Email_Exists()                                              
     {                                                                                                                    
         // Arrange                                                                                                       
         var request = new RegisterRequest                                                                                
         {                                                                                                                
             Email = "existing@example.com",                                                                              
             Password = "password123",                                                                                    
             FirstName = "John",                                                                                          
             LastName = "Doe"                                                                                             
         };                                                                                                               
                                                                                                                          
         _authServiceMock                                                                                                 
             .Setup(s => s.RegisterAsync(request.Email, request.Password, request.FirstName, request.LastName))           
             .ReturnsAsync((false, (User?)null, "Email already registered"));                                             
                                                                                                                          
         // Act                                                                                                           
         var result = await _controller.Register(request);                                                                
                                                                                                                          
         // Assert                                                                                                        
         result.Result.Should().BeOfType<BadRequestObjectResult>();                                                       
     }                                                                                                                    
                                                                                                                          
     // --- LOGIN ---                                                                                                     
                                                                                                                          
     [Fact]                                                                                                               
     public async Task Login_Should_Return_Ok_With_AuthResponse_On_Success()                                              
     {                                                                                                                    
         // Arrange                                                                                                       
         var request = new LoginRequest { Email = "test@example.com", Password = "password123" };                         
         var user = new User                                                                                              
         {                                                                                                                
             Email = request.Email,                                                                                       
             FirstName = "John",                                                                                          
             LastName = "Doe",                                                                                            
             PasswordHash = "hashedpassword"                                                                              
         };                                                                                                               
                                                                                                                          
         _authServiceMock                                                                                                 
             .Setup(s => s.LoginAsync(request.Email, request.Password))                                                   
             .ReturnsAsync((true, user, (string?)null));                                                                  
                                                                                                                          
         // Act                                                                                                           
         var result = await _controller.Login(request);                                                                   
                                                                                                                          
         // Assert                                                                                                        
         var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;                                        
         var response = okResult.Value.Should().BeOfType<AuthResponse>().Subject;                                         
         response.Token.Should().NotBeNullOrEmpty();                                                                      
         response.User.Email.Should().Be(request.Email);                                                                  
     }                                                                                                                    
                                                                                                                          
     [Fact]                                                                                                               
     public async Task Login_Should_Return_Unauthorized_With_Invalid_Credentials()                                        
     {                                                                                                                    
         // Arrange                                                                                                       
         var request = new LoginRequest { Email = "wrong@example.com", Password = "wrong" };                              
                                                                                                                          
         _authServiceMock                                                                                                 
             .Setup(s => s.LoginAsync(request.Email, request.Password))                                                   
             .ReturnsAsync((false, (User?)null, "Invalid credentials"));                                                  
                                                                                                                          
         // Act                                                                                                           
         var result = await _controller.Login(request);                                                                   
                                                                                                                          
         // Assert                                                                                                        
         result.Result.Should().BeOfType<UnauthorizedObjectResult>();                                                     
     }                                                                                                                    
                                                                                                                          
     // --- GET CURRENT USER ---                                                                                          
                                                                                                                          
     [Fact]                                                                                                               
     public async Task GetCurrentUser_Should_Return_Ok_When_Authenticated()                                               
     {                                                                                                                    
         // Arrange — simular un usuario autenticado configurando ClaimsPrincipal                                         
         var userId = Guid.NewGuid();                                                                                     
         var user = new User                                                                                              
         {                                                                                                                
             Id = userId,                                                                                                 
             Email = "test@example.com",                                                                                  
             FirstName = "John",                                                                                          
             LastName = "Doe",                                                                                            
             PasswordHash = "hashedpassword"                                                                              
         };                                                                                                               
                                                                                                                          
         _authServiceMock                                                                                                 
             .Setup(s => s.GetUserByIdAsync(userId))                                                                      
             .ReturnsAsync(user);                                                                                         
                                                                                                                          
         // Crear ClaimsPrincipal con el claim NameIdentifier (simula el JWT decodificado)                                
         var claims = new List<Claim>                                                                                     
         {                                                                                                                
             new Claim(ClaimTypes.NameIdentifier, userId.ToString())                                                      
         };                                                                                                               
         var identity = new ClaimsIdentity(claims, "TestAuth");                                                           
         var claimsPrincipal = new ClaimsPrincipal(identity);                                                             
                                                                                                                          
         // Asignar el ClaimsPrincipal al ControllerContext                                                               
         _controller.ControllerContext = new ControllerContext                                                            
         {                                                                                                                
             HttpContext = new DefaultHttpContext { User = claimsPrincipal }                                              
         };                                                                                                               
                                                                                                                          
         // Act                                                                                                           
         var result = await _controller.GetCurrentUser();                                                                 
                                                                                                                          
         // Assert                                                                                                        
         var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;                                        
         var dto = okResult.Value.Should().BeOfType<UserDto>().Subject;                                                   
         dto.Id.Should().Be(userId);                                                                                      
         dto.Email.Should().Be(user.Email);                                                                               
     }                                                                                                                    
                                                                                                                          
     [Fact]                                                                                                               
     public async Task GetCurrentUser_Should_Return_Unauthorized_Without_Claim()                                          
     {                                                                                                                    
         // Arrange — HttpContext sin claims (simula peticion sin token)                                                  
         _controller.ControllerContext = new ControllerContext                                                            
         {                                                                                                                
             HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }                                        
         };                                                                                                               
                                                                                                                          
         // Act                                                                                                           
         var result = await _controller.GetCurrentUser();                                                                 
                                                                                                                          
         // Assert                                                                                                        
         result.Result.Should().BeOfType<UnauthorizedResult>();                                                           
     }                                                                                                                    
                                                                                                                          
     [Fact]                                                                                                               
     public async Task GetCurrentUser_Should_Return_NotFound_When_User_Deleted()                                          
     {                                                                                                                    
         // Arrange — claim valido pero usuario ya no existe en BD                                                        
         var userId = Guid.NewGuid();                                                                                     
                                                                                                                          
         _authServiceMock                                                                                                 
             .Setup(s => s.GetUserByIdAsync(userId))                                                                      
             .ReturnsAsync((User?)null);                                                                                  
                                                                                                                          
         var claims = new List<Claim>                                                                                     
         {                                                                                                                
             new Claim(ClaimTypes.NameIdentifier, userId.ToString())                                                      
         };                                                                                                               
         var identity = new ClaimsIdentity(claims, "TestAuth");                                                           
         _controller.ControllerContext = new ControllerContext                                                            
         {                                                                                                                
             HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }                                
         };                                                                                                               
                                                                                                                          
         // Act                                                                                                           
         var result = await _controller.GetCurrentUser();                                                                 
                                                                                                                          
         // Assert                                                                                                        
         result.Result.Should().BeOfType<NotFoundResult>();                                                               
     }                                                                                                                    
 } 