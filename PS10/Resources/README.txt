PS10 Log
Jared Jensen and Ryan Welling

Database Description
- 3 tables: Games, Words, Players
	- Games: GameID, Player1, Player2, Player1Score, Player2Score, DateTime, Board, TimeLimit
		- GameID: AI, PK
	- Words: Word, Player, Game, Status
	- Players: Name, Won, Tied, Lost
		- Name: PK

Queries:
	Insert: command.CommandText = string.Format("INSERT INTO `cs3500_welling`.`Players` (`Name`, `Won`, `Tied`, `Lost`) VALUES ('{0}', '1', '0', '0');", Player1.Name);
	Update: string.Format("UPDATE `cs3500_welling`.`Players` SET `{0}` = `{0}` + 1 WHERE `Name`='{1}';", player1Outcome, Player1.Name);
