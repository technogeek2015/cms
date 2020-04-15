﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheManager.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using SSCMS.Dto;
using SSCMS.Plugins;
using SSCMS.Services;
using SSCMS.Utils;

namespace SSCMS.Web.Controllers.Admin.Plugins
{
    [OpenApiIgnore]
    [Authorize(Roles = Constants.RoleTypeAdministrator)]
    [Route(Constants.ApiAdminPrefix)]
    public partial class ManageController : ControllerBase
    {
        private const string Route = "plugins/manage";
        private const string RoutePluginId = "plugins/manage/{pluginId}";
        private const string RouteActionsReload = "plugins/manage/actions/reload";
        private const string RoutePluginIdEnable = "plugins/manage/{pluginId}/actions/enable";

        private readonly ICacheManager<object> _cacheManager;
        private readonly ISettingsManager _settingsManager;
        private readonly IAuthManager _authManager;
        private readonly IPluginManager _pluginManager;

        public ManageController(ICacheManager<object> cacheManager, ISettingsManager settingsManager, IAuthManager authManager, IPluginManager pluginManager)
        {
            _cacheManager = cacheManager;
            _settingsManager = settingsManager;
            _authManager = authManager;
            _pluginManager = pluginManager;
        }

        [HttpGet, Route(Route)]
        public async Task<ActionResult<GetResult>> Get()
        {
            if (!await _authManager.HasSystemPermissionsAsync(Constants.AppPermissions.PluginsManagement))
            {
                return Unauthorized();
            }

            //var dict = await _pluginManager.GetPluginIdAndVersionDictAsync();
            //var list = dict.Keys.ToList();
            //var packageIds = Utilities.ToString(list);

            var pluginIds = _pluginManager.Plugins.Select(x => x.PluginId);
            var enabledPlugins = _pluginManager.Plugins.Where(x => x.Disabled == false);

            return new GetResult
            {
                IsNightly = _settingsManager.IsNightlyUpdate,
                Version = _settingsManager.Version,
                EnabledPlugins = enabledPlugins,
                PluginIds = pluginIds
            };
        }

        [HttpDelete, Route(RoutePluginId)]
        public async Task<ActionResult<BoolResult>> Delete(string pluginId)
        {
            if (!await _authManager.HasSystemPermissionsAsync(Constants.AppPermissions.PluginsManagement))
            {
                return Unauthorized();
            }

            var plugin = _pluginManager.GetPlugin(pluginId);
            var pluginPath = PathUtils.Combine(_pluginManager.DirectoryPath, plugin.FolderName);
            DirectoryUtils.DeleteDirectoryIfExists(pluginPath);

            _pluginManager.Reload();

            await _authManager.AddAdminLogAsync("删除插件", $"插件:{pluginId}");

            _cacheManager.Clear();

            return new BoolResult
            {
                Value = true
            };
        }

        [HttpPost, Route(RouteActionsReload)]
        public async Task<ActionResult<BoolResult>> Reload()
        {
            if (!await _authManager.HasSystemPermissionsAsync(Constants.AppPermissions.PluginsManagement))
            {
                return Unauthorized();
            }

            _pluginManager.Reload();

            return new BoolResult
            {
                Value = true
            };
        }

        [HttpPost, Route(RoutePluginIdEnable)]
        public async Task<ActionResult<BoolResult>> Enable(string pluginId)
        {
            if (!await _authManager.HasSystemPermissionsAsync(Constants.AppPermissions.PluginsManagement))
            {
                return Unauthorized();
            }

            var config = new Dictionary<string, object>
            {
                [nameof(IPlugin.Disabled)] = false
            };
            await _pluginManager.SaveConfigAsync(pluginId, config);

            await _authManager.AddAdminLogAsync("启用插件", $"插件:{pluginId}");

            _cacheManager.Clear();

            return new BoolResult
            {
                Value = true
            };
        }
    }
}
