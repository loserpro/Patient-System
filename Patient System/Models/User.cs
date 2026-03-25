using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Patient_System.Models
{
    [Table("user")] // 指定数据库表名
    public class User
    {
        // 主键 + 自增
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // 登录用户名（唯一、非空）
        [Required(ErrorMessage = "用户名不能为空")]
        [MaxLength(50, ErrorMessage = "用户名最长50个字符")]
        public string Username { get; set; } = string.Empty;

        // 密码（非空，建议加密存储）
        [Required(ErrorMessage = "密码不能为空")]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        // 身份证号（18位、非空）
        [Required(ErrorMessage = "身份证号不能为空")]
        [MaxLength(18, ErrorMessage = "身份证号必须是18位")]
        [Column("id_card")] // 数据库列名（下划线命名）
        public string IdCard { get; set; } = string.Empty;

        // 真实姓名
        [Required(ErrorMessage = "姓名不能为空")]
        [MaxLength(50)]
        [Column("real_name")]
        public string RealName { get; set; } = string.Empty;

        // 性别：1=男，2=女，0=未知
        [Required(ErrorMessage = "性别不能为空")]
        public int Gender { get; set; }
    }
}
