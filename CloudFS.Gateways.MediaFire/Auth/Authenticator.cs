/*
The MIT License(MIT)

Copyright(c) 2015 IgorSoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using MediaFireSDK;
using MediaFireSDK.Core;
using MediaFireSDK.Model;
using MediaFireSDK.Model.Errors;
using MediaFireSDK.Model.Responses;
using IgorSoft.CloudFS.Authentication;

namespace IgorSoft.CloudFS.Gateways.MediaFire.Auth
{
    internal static class Authenticator
    {
        private class SynchronizationContext
        {
            private readonly IList<IMediaFireUserApi> contextHolders = new List<IMediaFireUserApi>();

            public AuthenticationContext LatestContext { get; private set; }

            public bool ContextUpdated { get; private set; }

            public string SettingsPassPhrase { get; }

            public SynchronizationContext(IMediaFireUserApi contextHolder, string settingsPassPhrase)
            {
                contextHolders.Add(contextHolder);
                LatestContext = contextHolder.GetAuthenticationContext();
                contextHolder.AuthenticationContextChanged += UpdateContexts;
                SettingsPassPhrase = settingsPassPhrase;
            }

            public void UpdateContexts(object source, EventArgs eventArgs)
            {
                LatestContext = ((IMediaFireUserApi)source).GetAuthenticationContext();
                ContextUpdated = true;
                foreach (var contextHolder in contextHolders)
                    if (source != contextHolder)
                        contextHolder.SetAuthenticationContext(LatestContext);
            }

            public void AttachContextHolder(IMediaFireUserApi contextHolder)
            {
                contextHolder.SetAuthenticationContext(LatestContext);
                contextHolders.Add(contextHolder);
                contextHolder.AuthenticationContextChanged += UpdateContexts;
            }
        }

        private static DirectLogOn logOn;

        private static readonly IDictionary<string, SynchronizationContext> contextDirectory = new Dictionary<string, SynchronizationContext>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static Authenticator()
        {
            AppDomain.CurrentDomain.DomainUnload += ShutdownHandler<EventArgs>;
            AppDomain.CurrentDomain.UnhandledException += ShutdownHandler<UnhandledExceptionEventArgs>;
        }

        private static void ShutdownHandler<T>(object sender, T eventArgs)
            where T : EventArgs
        {
            foreach (var item in contextDirectory)
                if (item.Value.ContextUpdated)
                    SaveRefreshToken(item.Key, item.Value.LatestContext, item.Value.SettingsPassPhrase);
        }

        private static AuthenticationContext LoadRefreshToken(string account, string settingsPassPhrase)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            var setting = refreshTokens?.SingleOrDefault(s => s.Account == account);
            return setting != null
                ? new AuthenticationContext(setting.SessionToken.DecryptUsing(settingsPassPhrase), long.Parse(setting.SecretKey.DecryptUsing(settingsPassPhrase)), setting.Time.DecryptUsing(settingsPassPhrase))
                : null;
        }

        internal static void SaveRefreshToken(string account, AuthenticationContext refreshToken, string settingsPassPhrase)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            if (refreshTokens != null) {
                var setting = refreshTokens.SingleOrDefault(s => s.Account == account);
                if (setting != null)
                    refreshTokens.Remove(setting);
            } else {
                refreshTokens = new System.Collections.ObjectModel.Collection<RefreshTokenSetting>();
                Properties.Settings.Default.RefreshTokens = refreshTokens;
            }

            refreshTokens.Insert(0, new RefreshTokenSetting() { Account = account, SessionToken = refreshToken.SessionToken.EncryptUsing(settingsPassPhrase), SecretKey = refreshToken.SecretKey.ToString().EncryptUsing(settingsPassPhrase), Time = refreshToken.Time.EncryptUsing(settingsPassPhrase) });

            Properties.Settings.Default.Save();
        }

        private static async Task<AuthenticationContext> RefreshSessionTokenAsync(IMediaFireAgent agent)
        {
            try {
                await agent.GetAsync<MediaFireGetUserInfoResponse>(MediaFireApiUserMethods.GetInfo);

                return agent.User.GetAuthenticationContext();
            } catch (MediaFireApiException) {
                return null;
            }
        }

        private static string GetAuthCode(string account)
        {
            var authCode = string.Empty;

            if (logOn == null)
                logOn = new DirectLogOn(AsyncOperationManager.SynchronizationContext);

            EventHandler<AuthenticatedEventArgs> callback = (s, e) => authCode = string.Join(",", e.Parameters.Get("account"), e.Parameters.Get("password"));
            logOn.Authenticated += callback;

            logOn.Show("Mediafire", account);

            logOn.Authenticated -= callback;

            return authCode;
        }

        public static async Task<MediaFireAgent> LoginAsync(string account, string code, string settingsPassPhrase)
        {
            if (string.IsNullOrEmpty(account))
                throw new ArgumentNullException(nameof(account));

            var agent = new MediaFireAgent(new MediaFireApiConfiguration(Secrets.API_KEY, Secrets.APP_ID, useHttpV1: true, automaticallyRenewToken: false));

            if (contextDirectory.TryGetValue(account, out SynchronizationContext synchronizationContext)) {
                synchronizationContext.AttachContextHolder(agent.User);
            } else {
                var refreshToken = LoadRefreshToken(account, settingsPassPhrase);

                if (refreshToken != null) {
                    agent.User.SetAuthenticationContext(refreshToken);

                    refreshToken = await RefreshSessionTokenAsync(agent);
                }

                if (refreshToken == null) {
                    if (string.IsNullOrEmpty(code))
                        code = GetAuthCode(account);

                    var parts = code?.Split(new[] { ',' }, 2) ?? Array.Empty<string>();
                    if (parts.Length != 2)
                        throw new AuthenticationException(string.Format(CultureInfo.CurrentCulture, Properties.Resources.ProvideAuthenticationData, account));

                    await agent.User.GetSessionToken(parts[0], parts[1], TokenVersion.V2);
                }

                synchronizationContext = new SynchronizationContext(agent.User, settingsPassPhrase);
                contextDirectory.Add(account, synchronizationContext);
            }

            return agent;
        }

        public static void PurgeRefreshToken(string account)
        {
            var refreshTokens = Properties.Settings.Default.RefreshTokens;
            if (refreshTokens == null)
                return;

            var settings = refreshTokens.Where(s => account == null || s.Account == account).ToArray();
            foreach (var setting in settings)
                refreshTokens.Remove(setting);
            if (!refreshTokens.Any())
                Properties.Settings.Default.RefreshTokens = null;
            Properties.Settings.Default.Save();
        }
    }
}
