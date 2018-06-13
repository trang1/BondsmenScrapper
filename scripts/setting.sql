-- Скрипт сгенерирован Devart dbForge Studio for MySQL, Версия 3.10.227.1
-- Дата: 6/14/2018 12:29:41 AM
-- Версия сервера: 5.5.23
-- Версия клиента: 4.1

CREATE TABLE bondsman.setting(
  Case_GUID VARCHAR (32) DEFAULT NULL,
  `Date` DATETIME NOT NULL,
  Court VARCHAR (16) DEFAULT NULL,
  Post_Jdgm VARCHAR (32) DEFAULT NULL,
  Docket_Type VARCHAR (32) DEFAULT NULL,
  Reason VARCHAR (64) DEFAULT NULL,
  Results VARCHAR (64) DEFAULT NULL,
  Defendant VARCHAR (64) DEFAULT NULL,
  Future_Date DATETIME DEFAULT NULL,
  Comments VARCHAR (128) DEFAULT NULL,
  Attorney_Appearance_Indicator VARCHAR (64) DEFAULT NULL,
  INDEX setting_FK1 USING BTREE (Case_GUID),
  CONSTRAINT setting_FK1 FOREIGN KEY (Case_GUID)
  REFERENCES bondsman.case_summary (Case_GUID)
)
ENGINE = INNODB
CHARACTER SET utf8
COLLATE utf8_general_ci;


