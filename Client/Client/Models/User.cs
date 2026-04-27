using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Models; 
public enum UserRole
{
    Operator,
    Admin
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Operator;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<FeedingEvent> FeedingEvents { get; set; } = new List<FeedingEvent>();
    public ICollection<EventComment> EventComments { get; set; } = new List<EventComment>();
}