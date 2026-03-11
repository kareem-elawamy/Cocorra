using Cocorra.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cocorra.DAL.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<RoomParticipant> RoomParticipants { get; set; }
        public DbSet<RoomTopicRequest> RoomTopicRequests { get; set; }
        public DbSet<TopicVote> TopicVotes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<FriendRequest> FriendRequests { get; set; }
        public DbSet<RoomReminder> RoomReminders { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ============================================================
            // 1. إعدادات جدول المشاركين (RoomParticipant)
            // ============================================================

            // ⚠️ ده السطر اللي كان ناقص وحل المشكلة
            // بنقوله إن المفتاح هو (RoomId + UserId) مع بعض
            builder.Entity<RoomParticipant>()
                .HasKey(p => new { p.RoomId, p.UserId });

            // العلاقات (Cascade & Restrict)
            builder.Entity<RoomParticipant>()
                .HasOne(p => p.Room)
                .WithMany(r => r.Participants)
                .HasForeignKey(p => p.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RoomParticipant>()
                .HasOne(p => p.User)
                .WithMany(u => u.RoomParticipations)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ============================================================
            // 2. إعدادات جدول التصويت (TopicVote)
            // ============================================================

            // ⚠️ نفس الكلام هنا، مفتاح مركب
            builder.Entity<TopicVote>()
                .HasKey(v => new { v.UserId, v.TopicRequestId });

            builder.Entity<TopicVote>()
                .HasOne(v => v.TopicRequest)
                .WithMany()
                .HasForeignKey(v => v.TopicRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TopicVote>()
                .HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ============================================================
            // 3. إعدادات جدول الغرفة (Room)
            // ============================================================
            builder.Entity<Room>()
                .HasOne(r => r.Host)
                .WithMany(u => u.OwnedRooms)
                .HasForeignKey(r => r.HostId)
                .OnDelete(DeleteBehavior.Restrict);


            // ============================================================
            // 4. إعدادات جدول طلبات المواضيع (RoomTopicRequest)
            // ============================================================
            builder.Entity<RoomTopicRequest>()
                .HasOne(r => r.Requester)
                .WithMany()
                .HasForeignKey(r => r.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RoomTopicRequest>()
                .HasOne(r => r.TargetCoach)
                .WithMany()
                .HasForeignKey(r => r.TargetCoachId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<RoomReminder>()
        .HasKey(rr => new { rr.UserId, rr.RoomId });

            // 2. تظبيط علاقات جدول الصداقة (عشان EF Core ميتلخبطش بين الـ Sender والـ Receiver)
            builder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany()
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.Restrict); // يفضل Restrict عشان لو يوزر اتمسح ميطيرش اليوزر التاني

            builder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany()
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}