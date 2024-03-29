﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using ABDataManager.Library.DataAccess;
using ABDataManager.Library.Models;
using ABDataManager.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace ABDataManager.Controllers
{
    //[Authorize]
    public class UserController : ApiController
    {
        [HttpGet]
        public UserModel GetById()
        {
            string userId = RequestContext.Principal.Identity.GetUserId();
            UserData data = new UserData();

           return data.GetUserById(userId).First();

        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("api/User/Admin/GetAllUsers")]
        public List<ApplicationUserModel> GetAllUsers()
        {
            List<ApplicationUserModel> output = new List<ApplicationUserModel>();

            using(var context = new ApplicationDbContext())
            {
                var userStore = new UserStore<ApplicationUser>(context);
                var userManager = new UserManager<ApplicationUser>(userStore);

                var users = userManager.Users.ToList();
                var roles = context.Roles.ToList();

                foreach (var user in users)
                {
                    ApplicationUserModel applicationUserModel = new ApplicationUserModel
                    {
                        Id = user.Id,
                        Email = user.Email,
                    };

                    foreach (var role in user.Roles)
                    {
                        applicationUserModel.Roles.Add(role.RoleId, roles.Where(x => x.Id == role.RoleId).First().Name);
                    }

                    output.Add(applicationUserModel);
                }
            }

            return output;
        }

        //[Authorize(Roles = "Admin")]
        [HttpGet]
        [Route("api/User/Admin/GetAllRoles")]
        public Dictionary<string, string> GetAllRoles()
        {
            using (var context = new ApplicationDbContext())
            {
                var roles = context.Roles.ToDictionary(x => x.Id, x => x.Name);

                return roles;
            }
        }

        //[Authorize(Roles = "Admin")]
        [HttpPost]
        [Route("api/User/Admin/AddRoleToUser")]
        public void AddRoleToUser(UserRolePairModel userRolepair)
        {
            using (var context = new ApplicationDbContext())
            {
                var userStore = new UserStore<ApplicationUser>(context);
                var userManager = new UserManager<ApplicationUser>(userStore);

                userManager.AddToRole(userRolepair.UserId, userRolepair.RoleName);
            }
        }

        //[Authorize(Roles = "Admin")]
        [HttpPost]
        [Route("api/User/Admin/RemoveRoleFromUser")]
        public void RemoveRoleFromUser(UserRolePairModel userRolepair)
        {
            using (var context = new ApplicationDbContext())
            {
                var userStore = new UserStore<ApplicationUser>(context);
                var userManager = new UserManager<ApplicationUser>(userStore);

                userManager.RemoveFromRole(userRolepair.UserId, userRolepair.RoleName);
            }
        }
    }
}