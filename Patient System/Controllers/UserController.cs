/*
 *未实现拦截请求
 */

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Patient_System.Data;
using Patient_System.Models;

namespace Patient_System.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        //注入DbContext（依赖注入）
        private readonly AppDbContext _dbContext;
        private readonly IConfiguration _configuration; // 令牌   

        //构造的函数
        public UserController(AppDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        //折叠代码
        #region 1. 新增用户（注册）
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            // 1. 模型验证（ApiController 自动验证，也可手动检查）
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 2. 检查用户名是否已存在
            if (await _dbContext.Users.AnyAsync(u => u.Username == user.Username))
            {
                return Conflict("用户名已存在");
            }

            //3.添加并保存
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            Console.WriteLine("====新增了一个用户=====");

            //4.result风格返回创建成功（201）+ 用户信息（隐藏密码）
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id },
                new { user.Id, user.Username, user.RealName, user.IdCard, user.Gender });
        }
        #endregion

        #region 2. 根据ID查询用户
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            Console.WriteLine("====查询用户=====");
            // 隐藏密码返回
            return Ok(new { user.Id, user.Username, user.RealName, user.IdCard, user.Gender });
        }
        #endregion

       

        #region 4. 修改用户信息
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
        {
            if (id != user.Id)
            {
                return BadRequest("ID不匹配");
            }

            // 检查用户是否存在
            var existingUser = await _dbContext.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound("用户不存在");
            }

            // 更新字段（仅更新允许修改的字段，密码单独处理）
            existingUser.RealName = user.RealName;
            existingUser.IdCard = user.IdCard;
            existingUser.Gender = user.Gender;

            // 如果传了新密码，加密后更新
            if (!string.IsNullOrEmpty(user.Password))
            {
                existingUser.Password = user.Password;
            }

            await _dbContext.SaveChangesAsync();

            Console.WriteLine("====修改成功=====");

            return NoContent(); // 204：更新成功无返回内容
        }
        #endregion

        #region 5. 删除用户
        [Authorize] // 添加鉴权特性
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // 1. 从JWT令牌中获取当前登录用户的username
            //var currentUsername = User.FindFirst("unique_name")?.Value;
            var currentUsername = User.FindFirst(ClaimTypes.Name)?.Value;
            // 2. 验证：只有username为admin的用户才能执行删除
            if (currentUsername != "admin")
            {
                return Forbid("仅管理员账号（admin）有权限执行删除操作"); // 403禁止访问
            }

            var user = await _dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("用户不存在");
            }

            // 4.禁止admin删除自己（避免误删管理员账号）
            if (user.Username == "admin")
            {
                return BadRequest("禁止删除管理员账号（admin）");
            }

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();

            Console.WriteLine("====删除了一个用户=====");

            return NoContent();
        }
        #endregion

        //用户列表查询,未实现分页查询
        [HttpGet("list")]
        public async Task<IActionResult> listUser()
        {
            var users = await _dbContext.Users.ToListAsync();

            if(users == null || !users.Any())
            {
                return NotFound("没有找到任何用户");
            }

            Console.WriteLine("===查询所有用户===");

            // 同样隐藏密码，只返回需要的字段
            var result = users.Select(u => new
            {
                u.Id,
                u.Username,
                u.RealName,
                u.IdCard,
                u.Gender
            }).ToList();

            return Ok(result);
        }

        #region 6. 用户登录（用户名+密码验证）
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // 1. 自动模型验证（ApiController特性），验证失败返回400
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 2. 根据用户名查询用户，包含密码（用于验证，不返回前端）
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            // 3. 用户名不存在，返回404
            if (user == null)
            {
                return NotFound("用户名或密码错误"); // 统一提示，避免暴露用户存在性
            }

            // 4. 验证密码：BCrypt对比明文密码和数据库加密密码
            bool isPwdValid = loginDto.Password == user.Password;
            if (!isPwdValid)
            {
                return Unauthorized("用户名或密码错误"); // 401未授权
            }

            // ======== 新增：生成JWT Token ========
            // 读取JWT配置（和Program.cs里的配置对应）
            var jwtKey = _configuration["Jwt:Key"] ?? "your_secret_key_123456";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "AspNetCore_EFCore_MySQL";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "UserClient";
            var key = Encoding.UTF8.GetBytes(jwtKey);

            // 创建Token的身份信息（包含用户名、用户ID，用于后续鉴权）
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.Name, user.Username), // 存储用户名（删除接口会用到）
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) // 存储用户ID
        }),
                Expires = DateTime.UtcNow.AddHours(2), // Token有效期2小时
                Issuer = jwtIssuer,
                Audience = jwtAudience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            // 生成Token字符串
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            Console.WriteLine("====登录成功====="); 
            // 5. 登录成功，返回用户基础信息（隐藏密码、IDCard等敏感字段可按需屏蔽）
            return Ok(new
            {
                Code = 200,
                Message = "登录成功",
                Token = tokenString, // 返回Token（前端需要存储）
                Expires = DateTime.UtcNow.AddHours(2).ToString("yyyy-MM-dd HH:mm:ss"), // Token过期时间
                Data = new
                {
                    user.Id,
                    user.Username,
                    user.RealName,
                    user.Gender
                }
            });
        }
        #endregion
    }
}
