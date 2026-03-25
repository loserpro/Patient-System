using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Patient_System.Models;

namespace Patient_System.Data
{
    public class AppDbContext:DbContext
    {
        //对应user表的数据集
        public DbSet<User> Users { get; set; }

        //注入配置（读取连接字符串）
        private readonly IConfiguration _confiuration;

        public AppDbContext(IConfiguration configuration)
        {
            _confiuration = configuration;
        }

        //配置mysql连接
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connStr = _confiuration.GetConnectionString("MySqlConnection");
            //自动检测MySql版本，适配不同环境
            optionsBuilder.UseMySql(connStr, ServerVersion.AutoDetect(connStr));
        }

        //配置表约束（如用户名唯一索引）
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 给 Username 添加唯一索引，防止重复注册
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
        }
    }
}
