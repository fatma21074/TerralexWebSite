using Microsoft.AspNetCore.Identity;
using TerralexApp.Models;

namespace TerralexAPP.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            // Ensure Database is created and migrations are applied
            await context.Database.MigrateAsync();

            // To Add Initial Roles
            string[] roleNames = { "Admin", "Manager", "Staff", "Client" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new ApplicationRole
                    {
                        Name = roleName
                    };
                    await roleManager.CreateAsync(role);
                }
            }

            // To Add Initial Admin User
            var adminEmail = "admin@Terralex.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (result.Succeeded)
                {
                    // To Assign Admin Role to the Admin User
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Cities
            if (!await context.Cities.AnyAsync())
            {
                var cities = new List<City>
                {
                    new City { Name = "Cairo", IsDeleted = false },
                    new City { Name = "Giza", IsDeleted = false },
                    new City { Name = "Alexandria", IsDeleted = false },
                    new City { Name = "Mansoura", IsDeleted = false },
                    new City { Name = "Luxor", IsDeleted = false },
                    new City { Name = "Aswan", IsDeleted = false }
                };
                await context.Cities.AddRangeAsync(cities);
                await context.SaveChangesAsync();
            }

            // Seed Property Categories & Types
            if (!await context.ProppertyCategories.AnyAsync())
            {
                var residential = new ProppertyCategory { Name = "Residential", IsDeleted = false };
                var commercial = new ProppertyCategory { Name = "Commercial", IsDeleted = false };
                var land = new ProppertyCategory { Name = "Land", IsDeleted = false };

                await context.ProppertyCategories.AddRangeAsync(residential, commercial, land);
                await context.SaveChangesAsync();

                var types = new List<PropertyType>
                {
                    new PropertyType { Name = "Apartment", PropertyCategoryId = residential.PropertyCategoryId, IsDeleted = false },
                    new PropertyType { Name = "Villa", PropertyCategoryId = residential.PropertyCategoryId, IsDeleted = false },
                    new PropertyType { Name = "Office", PropertyCategoryId = commercial.PropertyCategoryId, IsDeleted = false },
                    new PropertyType { Name = "Retail Store", PropertyCategoryId = commercial.PropertyCategoryId, IsDeleted = false },
                    new PropertyType { Name = "Agricultural Land", PropertyCategoryId = land.PropertyCategoryId, IsDeleted = false }
                };
                await context.PropertyTypes.AddRangeAsync(types);
                await context.SaveChangesAsync();
            }

            // Seed Service Types
            if (!await context.ServerTypes.AnyAsync())
            {
                var serviceTypes = new List<ServerType>
                {
                    new ServerType { Name = "Contract Writing", IsDeleted = false },
                    new ServerType { Name = "Property Registration", IsDeleted = false },
                    new ServerType { Name = "Ownership Verification", IsDeleted = false },
                    new ServerType { Name = "Legal Consultation", IsDeleted = false }
                };
                await context.ServerTypes.AddRangeAsync(serviceTypes);
                await context.SaveChangesAsync();
            }

            // Seed Transaction Statuses
            if (!await context.TransactionStatuses.AnyAsync())
            {
                var statuses = new List<TransactionStatus>
                {
                    new TransactionStatus { Name = "Draft", IsDeleted = false },
                    new TransactionStatus { Name = "In Progress", IsDeleted = false },
                    new TransactionStatus { Name = "Pending Signature", IsDeleted = false },
                    new TransactionStatus { Name = "Registered", IsDeleted = false },
                    new TransactionStatus { Name = "Closed", IsDeleted = false }
                };
                await context.TransactionStatuses.AddRangeAsync(statuses);
                await context.SaveChangesAsync();
            }

            // Seed Office Profile
            if (!await context.OfficeProfiles.AnyAsync())
            {
                var office = new OfficeProfile
                {
                    Address = "123 El-Tahrir Square, Cairo, Egypt",
                    ResponsabltyPerson = "Ashraf Terralex",
                    Phone = "+20212345678",
                    Moblie = "+201012345678",
                    Email = "info@terralex.com",
                    Website = "www.terralex.com",
                    LogoPath = "/assets/img/logo.png",
                    WorkStart = 9,
                    WorkEnd = 17,
                    IsDeleted = false
                };
                await context.OfficeProfiles.AddAsync(office);
                await context.SaveChangesAsync();

                // Seed Office Staff (Lawyers)
                if (!await context.OfficeStuffs.AnyAsync())
                {
                    var staff = new List<OfficeStuff>
                    {
                        new OfficeStuff { Name = "Ashraf Terralex", JobTitle = "Senior Partner & Real Estate Lawyer", OfficeId = office.OfficeId, IsDeleted = false },
                        new OfficeStuff { Name = "Fatma Lawyer", JobTitle = "Associate Real Estate Attorney", OfficeId = office.OfficeId, IsDeleted = false }
                    };
                    await context.OfficeStuffs.AddRangeAsync(staff);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}

