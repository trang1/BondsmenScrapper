-- ������ ������������ Devart dbForge Studio for MySQL, ������ 3.10.227.1
-- ����: 6/14/2018 12:04:29 AM
-- ������ �������: 5.5.23
-- ������ �������: 4.1

CREATE TABLE bondsman.activity(
  Case_Guid VARCHAR (32) NOT NULL,
  `Date` DATE NOT NULL,
  Type VARCHAR (64) NOT NULL,
  Description VARCHAR (512) DEFAULT NULL,
  SNU VARCHAR (32) DEFAULT NULL,
  INDEX activity_FK1 USING BTREE (Case_Guid)
)
ENGINE = INNODB
CHARACTER SET utf8
COLLATE utf8_general_ci;


