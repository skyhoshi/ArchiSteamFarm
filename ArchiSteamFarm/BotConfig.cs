﻿/*
    _                _      _  ____   _                           _____
   / \    _ __  ___ | |__  (_)/ ___| | |_  ___   __ _  _ __ ___  |  ___|__ _  _ __  _ __ ___
  / _ \  | '__|/ __|| '_ \ | |\___ \ | __|/ _ \ / _` || '_ ` _ \ | |_  / _` || '__|| '_ ` _ \
 / ___ \ | |  | (__ | | | || | ___) || |_|  __/| (_| || | | | | ||  _|| (_| || |   | | | | | |
/_/   \_\|_|   \___||_| |_||_||____/  \__|\___| \__,_||_| |_| |_||_|   \__,_||_|   |_| |_| |_|

 Copyright 2015-2017 Łukasz "JustArchi" Domeradzki
 Contact: JustArchi@JustArchi.net

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

 http://www.apache.org/licenses/LICENSE-2.0
					
 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.

*/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using ArchiSteamFarm.JSON;
using ArchiSteamFarm.Localization;
using Newtonsoft.Json;

namespace ArchiSteamFarm {
	[SuppressMessage("ReSharper", "ClassCannotBeInstantiated")]
	[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
	[SuppressMessage("ReSharper", "ConvertToConstant.Global")]
	internal sealed class BotConfig {
		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly bool AcceptGifts;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly bool CardDropsRestricted = true;

		[JsonProperty]
		internal readonly string CustomGamePlayedWhileFarming;

		[JsonProperty]
		internal readonly string CustomGamePlayedWhileIdle;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly bool DismissInventoryNotifications;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly bool Enabled;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly EFarmingOrder FarmingOrder = EFarmingOrder.Unordered;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly bool FarmOffline;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly HashSet<uint> GamesPlayedWhileIdle = new HashSet<uint>();

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly bool HandleOfflineMessages;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly bool IsBotAccount;

		[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace, Required = Required.DisallowNull)]
		internal readonly HashSet<Steam.Item.EType> LootableTypes = new HashSet<Steam.Item.EType> {
			Steam.Item.EType.BoosterPack,
			Steam.Item.EType.FoilTradingCard,
			Steam.Item.EType.TradingCard
		};

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly CryptoHelper.ECryptoMethod PasswordFormat = CryptoHelper.ECryptoMethod.PlainText;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly bool Paused;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly ERedeemingPreferences RedeemingPreferences = ERedeemingPreferences.None;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly bool SendOnFarmingFinished;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly byte SendTradePeriod;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly bool ShutdownOnFarmingFinished;

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly ulong SteamMasterClanID;

		[JsonProperty]
		internal readonly string SteamTradeToken;

		[SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly Dictionary<ulong, EPermission> SteamUserPermissions = new Dictionary<ulong, EPermission>();

		[JsonProperty(Required = Required.DisallowNull)]
		internal readonly ETradingPreferences TradingPreferences = ETradingPreferences.None;

		[JsonProperty]
		internal string SteamLogin { get; set; }

		[JsonProperty]
		internal string SteamParentalPIN { get; set; } = "0";

		[JsonProperty]
		internal string SteamPassword { get; set; }

		// This constructor is used only by deserializer
		private BotConfig() { }

		internal static BotConfig Load(string filePath) {
			if (string.IsNullOrEmpty(filePath)) {
				ASF.ArchiLogger.LogNullError(nameof(filePath));
				return null;
			}

			if (!File.Exists(filePath)) {
				return null;
			}

			BotConfig botConfig;

			try {
				botConfig = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(filePath));
			} catch (Exception e) {
				ASF.ArchiLogger.LogGenericException(e);
				return null;
			}

			if (botConfig == null) {
				ASF.ArchiLogger.LogNullError(nameof(botConfig));
				return null;
			}

			// Support encrypted passwords
			if ((botConfig.PasswordFormat != CryptoHelper.ECryptoMethod.PlainText) && !string.IsNullOrEmpty(botConfig.SteamPassword)) {
				// In worst case password will result in null, which will have to be corrected by user during runtime
				botConfig.SteamPassword = CryptoHelper.Decrypt(botConfig.PasswordFormat, botConfig.SteamPassword);
			}

			// User might not know what he's doing
			// Ensure that he can't screw core ASF variables
			if (botConfig.GamesPlayedWhileIdle.Count <= ArchiHandler.MaxGamesPlayedConcurrently) {
				return botConfig;
			}

			ASF.ArchiLogger.LogGenericWarning(string.Format(Strings.WarningTooManyGamesToPlay, ArchiHandler.MaxGamesPlayedConcurrently, nameof(botConfig.GamesPlayedWhileIdle)));

			HashSet<uint> validGames = new HashSet<uint>(botConfig.GamesPlayedWhileIdle.Take(ArchiHandler.MaxGamesPlayedConcurrently));
			botConfig.GamesPlayedWhileIdle.IntersectWith(validGames);
			botConfig.GamesPlayedWhileIdle.TrimExcess();

			return botConfig;
		}

		internal enum EFarmingOrder : byte {
			Unordered,
			AppIDsAscending,
			AppIDsDescending,
			CardDropsAscending,
			CardDropsDescending,
			HoursAscending,
			HoursDescending,
			NamesAscending,
			NamesDescending
		}

		internal enum EPermission : byte {
			None,
			FamilySharing,
			Operator,
			Master
		}

		[Flags]
		internal enum ERedeemingPreferences : byte {
			None = 0,
			Forwarding = 1,
			Distributing = 2,
			KeepMissingGames = 4
		}

		[Flags]
		internal enum ETradingPreferences : byte {
			None = 0,
			AcceptDonations = 1,
			SteamTradeMatcher = 2,
			MatchEverything = 4,
			DontAcceptBotTrades = 8
		}
	}
}