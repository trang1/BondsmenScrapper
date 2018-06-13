-- Скрипт сгенерирован Devart dbForge Studio for MySQL, Версия 3.10.227.1
-- Дата: 6/13/2018 11:49:05 PM
-- Версия сервера: 5.5.23
-- Версия клиента: 4.1

CREATE TABLE bondsman.case_summary(
  Case_Number VARCHAR (64) NOT NULL,
  File_Date DATE NOT NULL,
  Case_Status VARCHAR (64) DEFAULT NULL,
  Offense VARCHAR (64) DEFAULT NULL,
  Last_Instrument_Filed VARCHAR (64) DEFAULT NULL,
  Disposition VARCHAR (128) DEFAULT NULL,
  Completion_Date DATE DEFAULT NULL,
  Defendant_Status VARCHAR (64) DEFAULT NULL,
  Bond_Amount DECIMAL (10, 2) DEFAULT NULL,
  Setting_Date DATE DEFAULT NULL,
  Def_Race_Sex VARCHAR (16) DEFAULT NULL,
  Def_Eyes VARCHAR (16) DEFAULT NULL,
  Def_Skin VARCHAR (16) DEFAULT NULL,
  Def_DOB DATE DEFAULT NULL,
  Def_US_Citizen VARCHAR (16) DEFAULT NULL,
  Def_Address VARCHAR (512) DEFAULT NULL,
  Def_Markings VARCHAR (256) DEFAULT NULL,
  Def_Height_Weight VARCHAR (32) DEFAULT NULL,
  Def_Hair VARCHAR (16) DEFAULT NULL,
  Def_Build VARCHAR (16) DEFAULT NULL,
  Def_In_Custody VARCHAR (16) DEFAULT NULL,
  Def_Place_Of_Birth VARCHAR (32) DEFAULT NULL,
  CPJ_Current_Court VARCHAR (16) DEFAULT NULL,
  CPJ_Address VARCHAR (512) DEFAULT NULL,
  CPJ_Judge_Name VARCHAR (128) DEFAULT NULL,
  CPJ_Court_Type VARCHAR (32) DEFAULT NULL,
  Case_GUID VARCHAR (32),
  PRIMARY KEY (Case_GUID)
)
ENGINE = INNODB
CHARACTER SET utf8
COLLATE utf8_general_ci;


