using System.Collections.Generic;
using TerralexApp.Models;

namespace TerralexAPP.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalClients { get; set; }
        public int TotalProperties { get; set; }
        public int ActiveTransactions { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Appointment> UpcomingAppointments { get; set; } = new List<Appointment>();
        public List<Transaction> RecentTransactions { get; set; } = new List<Transaction>();
    }
}
