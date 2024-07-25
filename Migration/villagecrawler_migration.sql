SET SQL_MODE=ANSI_QUOTES;

alter table "Alliances" add "PlayerCount" int NOT NULL after "Name";

update Alliances
set Alliances.PlayerCount = (select count(*) from Players where AllianceId = Alliances.Id)
where Alliances.Id >= 0;

CREATE TABLE "AlliancesHistory" (
  "Id" int NOT NULL AUTO_INCREMENT,
  "AllianceId" int NOT NULL,
  "Date" datetime(6) NOT NULL,
  "PlayerCount" int NOT NULL,
  "ChangePlayerCount" int NOT NULL,
  PRIMARY KEY ("Id"),
  KEY "IX_AlliancesHistory_AllianceId_ChangePlayerCount" ("AllianceId","ChangePlayerCount"),
  KEY "IX_AlliancesHistory_AllianceId_Date" ("AllianceId","Date"),
  CONSTRAINT "FK_AlliancesHistory_Alliances_AllianceId" FOREIGN KEY ("AllianceId") REFERENCES "Alliances" ("Id") ON DELETE CASCADE
);

CREATE TABLE "PlayersHistory" (
  "Id" int NOT NULL AUTO_INCREMENT,
  "PlayerId" int NOT NULL,
  "Date" datetime(6) NOT NULL,
  "AllianceId" int NOT NULL,
  "ChangeAlliance" tinyint(1) NOT NULL,
  "Population" int NOT NULL,
  "ChangePopulation" int NOT NULL,
  PRIMARY KEY ("Id"),
  KEY "IX_PlayersHistory_PlayerId_ChangeAlliance" ("PlayerId","ChangeAlliance"),
  KEY "IX_PlayersHistory_PlayerId_ChangePopulation" ("PlayerId","ChangePopulation"),
  KEY "IX_PlayersHistory_PlayerId_Date" ("PlayerId","Date"),
  CONSTRAINT "FK_PlayersHistory_Players_PlayerId" FOREIGN KEY ("PlayerId") REFERENCES "Players" ("Id") ON DELETE CASCADE
);

insert into PlayersHistory ("Id", "PlayerId", "Date", "AllianceId", "ChangeAlliance", "Population", "ChangePopulation")
select "Id", "PlayerId", "Date", "AllianceId", "Change", 0, 0
from PlayerAllianceHistory
where Id >= 0;

update PlayersHistory
set PlayersHistory.Population = (select Population from PlayerPopulationHistory where PlayerId = PlayersHistory.PlayerId and "Date" = PlayersHistory.Date),
	PlayersHistory.ChangePopulation = (select "Change" from PlayerPopulationHistory where PlayerId = PlayersHistory.PlayerId and "Date" = PlayersHistory.Date)
where PlayersHistory.Id >= 0; 

drop table PlayerAllianceHistory;
drop table PlayerPopulationHistory;

alter table VillagePopulationHistory
rename column "Change" TO "ChangePopulation",
rename index "IX_VillagePopulationHistory_VillageId_Date" to "IX_VillagesHistory_VillageId_Date";

create index "IX_VillagesHistory_VillageId_ChangePopulation"
on "VillagePopulationHistory" ("VillageId","ChangePopulation");

rename table VillagePopulationHistory to VillagesHistory;