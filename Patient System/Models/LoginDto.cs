using System.ComponentModel.DataAnnotations;

namespace Patient_System.Models
{
    /// <summary>
    /// 登录请求参数模型
    /// </summary>
    public class LoginDto
    {
        /// <summary>
        /// 登录用户名
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [MaxLength(50, ErrorMessage = "用户名最长50个字符")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 登录密码（明文）
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        [MaxLength(100, ErrorMessage = "密码最长100个字符")]
        public string Password { get; set; } = string.Empty;
    }
}
