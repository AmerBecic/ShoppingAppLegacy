﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ABMDesktopUI.Library.Api;
using ABMDesktopUI.Library.Models;
using Caliburn.Micro;

namespace ABMDesktopUI.ViewModels
{
    public class UserViewModel : Screen
    {
        public BindingList<ApplicationUserModel> _users;
        private readonly IUserApi _userApi;
        private readonly StatusInfoViewModel _statusInfo;
        private readonly IWindowManager _window;

        public UserViewModel(StatusInfoViewModel statusInfo , IWindowManager window, IUserApi userApi)
        {
            _userApi = userApi;
            _statusInfo = statusInfo;
            _window = window;
        }

        protected override async void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            try
            {
                await LoadUsers();
            }
            catch (Exception ex)
            {
                dynamic settings = new ExpandoObject();
                settings.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                settings.ResizeMode = ResizeMode.NoResize;
                settings.Title = "System Error";

                if (ex.Message == "Unauthorized")
                {
                    _statusInfo.UpdateMessage("Unauthorized Access", "You do not have permission to interact with the Sales Form");
                    _window.ShowDialog(_statusInfo, null, settings);
                }
                else
                {
                    _statusInfo.UpdateMessage("Fatal Exception", ex.Message);
                    _window.ShowDialog(_statusInfo, null, settings);
                }
                TryClose();
            }
        }
        private async Task LoadUsers()
        {
            var usersList = await _userApi.GetAll();
            Users = new BindingList<ApplicationUserModel>(usersList);
        }

        private async Task LoadAvailableRoles()
        {
            var roles = await _userApi.GetAllRoles();

            foreach(var role in roles)
            {
                if (UserRoles.IndexOf(role.Value) < 0)
                {
                    AvailableRoles.Add(role.Value);
                }
            }
        }

        public BindingList<ApplicationUserModel> Users
        {
            get { return _users; }
            set
            {
                _users = value;
                NotifyOfPropertyChange(() => Users);
            }
        }

        private ApplicationUserModel _selectedUser;

        public ApplicationUserModel SelectedUser
        {
            get { return  _selectedUser; }
            set
            {
                _selectedUser = value;
                SelectedUserName = _selectedUser.Email;
                UserRoles = new BindingList<string>(_selectedUser.Roles.Select(x => x.Value).ToList());
                LoadAvailableRoles();
                NotifyOfPropertyChange(() => SelectedUser);
            }
        }

        private string _selectedUserRole;

        public string SelectedUserRole
        {
            get { return _selectedUserRole; }
            set
            {
                _selectedUserRole = value;
                NotifyOfPropertyChange(() => SelectedUserRole);
            }
        }

        private string _selectedAvaialbeRole;
        public string SelectedAvailableRole
        {
            get { return _selectedAvaialbeRole; }
            set
            {
                _selectedAvaialbeRole = value;
                NotifyOfPropertyChange(() => SelectedAvailableRole);
            }
        }

        private string _selectedUserName;

        public string SelectedUserName
        {
            get 
            { 
                return _selectedUserName;
            }
            set 
            {
                _selectedUserName = value;
                NotifyOfPropertyChange(() => SelectedUserName);
            }
        }

        private BindingList<string> _userRoles = new BindingList<string>();

        public BindingList<string> UserRoles
        {
            get { return _userRoles; }
            set 
            {
                _userRoles = value;
                NotifyOfPropertyChange(() => UserRoles);
            }
        }

        private BindingList<string> _availableRoles = new BindingList<string>();
        public BindingList<string> AvailableRoles
        {
            get { return _availableRoles; }
            set
            {
                _availableRoles = value;
                NotifyOfPropertyChange(() => AvailableRoles);
            }
        }

        public async void AddSelectedRole()
        {
            await _userApi.AddRoleToUser(SelectedUser.Id, SelectedAvailableRole);

            UserRoles.Add(SelectedAvailableRole);
            AvailableRoles.Remove(SelectedAvailableRole);

        }

        public async void RemoveSelectedRole()
        {
            await _userApi.RemoveRoleFromUser(SelectedUser.Id, SelectedUserRole);

            AvailableRoles.Add(SelectedUserRole);
            UserRoles.Remove(SelectedUserRole);
        }


    }
}
