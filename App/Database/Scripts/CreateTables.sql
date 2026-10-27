CREATE TABLE IF NOT EXISTS `Alliances` (
    `Id` INT NOT NULL,
    `Name` VARCHAR(255) NOT NULL DEFAULT '',
    `PlayerCount` INT NOT NULL,
    PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX IF NOT EXISTS `IX_Alliances_Name` ON `Alliances` (`Name`);

CREATE TABLE IF NOT EXISTS `Players` (
    `Id` INT NOT NULL,
    `AllianceId` INT NOT NULL,
    `Name` VARCHAR(255) NOT NULL DEFAULT '',
    `Population` INT NOT NULL,
    `VillageCount` INT NOT NULL,
    `IsDeleted` TINYINT(1) NOT NULL,
    PRIMARY KEY (`Id`),
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX IF NOT EXISTS `IX_Players_Name_Active` ON `Players` (`Name`, `IsDeleted`);
CREATE INDEX IF NOT EXISTS `IX_Players_Alliances_Active` ON `Players` (`AllianceId`, `IsDeleted`);

CREATE TABLE IF NOT EXISTS `Villages` (
    `Id` INT NOT NULL,
    `MapId` INT NOT NULL,
    `PlayerId` INT NOT NULL,
    `PreviousPlayerId` INT NULL DEFAULT NULL,
    `Name` VARCHAR(255) NOT NULL DEFAULT '',
    `X` INT NOT NULL,
    `Y` INT NOT NULL,
    `Tribe` INT NOT NULL,
    `Population` INT NOT NULL,
    `Region` VARCHAR(255) NOT NULL DEFAULT '',
    `IsCapital` TINYINT(1) NOT NULL,
    `IsCity` TINYINT(1) NOT NULL,
    `IsHarbor` TINYINT(1) NOT NULL,
    `VictoryPoints` INT NOT NULL,
    `IsDestroyed` TINYINT(1) NOT NULL DEFAULT 0, 
    PRIMARY KEY (`Id`),
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX IF NOT EXISTS `IX_Villages_Players_Active` ON `Villages` (`PlayerId`, `IsDestroyed`);
CREATE INDEX IF NOT EXISTS `IX_Villages_Name_Active` ON `Villages` (`Name`, `IsDestroyed`);
CREATE INDEX IF NOT EXISTS `IX_Villages_PreviousPlayerId` ON `Villages` (`PreviousPlayerId`);

CREATE TABLE IF NOT EXISTS `VillagesHistory` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `VillagesId` INT NOT NULL,
    `MapId` INT NOT NULL,
    `PlayerId` INT NULL,
    `SnapshotDate` DATE NOT NULL, 
    `Population` INT NOT NULL,
    `ChangePopulation` MEDIUMINT NOT NULL, 
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_VillagesHistory_Villages_VillagesId` FOREIGN KEY (`VillagesId`) REFERENCES `Villages` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE UNIQUE INDEX IF NOT EXISTS `UX_VillagesHistory_VillagesId_SnapshotDate` ON `VillagesHistory` (`VillagesId`, `SnapshotDate`);
CREATE INDEX IF NOT EXISTS `IX_VillagesHistory_MapId_SnapshotDate` ON `VillagesHistory` (`MapId`, `SnapshotDate`);
CREATE INDEX IF NOT EXISTS `IX_VillagesHistory_PlayerId_SnapshotDate` ON `VillagesHistory` (`PlayerId`, `SnapshotDate`);


CREATE TABLE IF NOT EXISTS `PlayersHistory` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `PlayerId` INT NOT NULL,
    `SnapshotDate` DATE NOT NULL,
    `AllianceId` INT NOT NULL,
    `ChangeAlliances` TINYINT(1) NOT NULL,
    `Population` INT NOT NULL,
    `ChangePopulation` MEDIUMINT NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PlayersHistory_Players` FOREIGN KEY (`PlayerId`) REFERENCES `Players` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE UNIQUE INDEX IF NOT EXISTS `UX_PlayersHistory_PlayerId_SnapshotDate` ON `PlayersHistory` (`PlayerId`, `SnapshotDate`);

CREATE TABLE IF NOT EXISTS `AlliancesHistory` (
    `Id` INT NOT NULL AUTO_INCREMENT,
    `AllianceId` INT NOT NULL,
    `SnapshotDate` DATE NOT NULL,
    `PlayerCount` INT NOT NULL,
    `ChangePlayerCount` SMALLINT NOT NULL,
    PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AlliancesHistory_Alliances` FOREIGN KEY (`AllianceId`) REFERENCES `Alliances` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE UNIQUE INDEX IF NOT EXISTS `UX_AlliancesHistory_AllianceId_SnapshotDate` ON `AlliancesHistory` (`AllianceId`, `SnapshotDate`);