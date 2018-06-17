using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using MySql.Data.Entity;

namespace BondsmenScrapper.Data
{
    // Code-Based Configuration and Dependency resolution
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public partial class DataContext : DbContext
    {
        public DbSet<Acitvity> Activities { get; set; }
        public DbSet<Bond> Bonds { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<CaseSummary> CaseSummaries { get; set; }
        public DbSet<CriminalHistory> CriminalHistories { get; set; }
        public DbSet<Hold> Holds { get; set; }
        public DbSet<Setting> Settings { get; set; }

        public DataContext() : base()
        {

        }

        // Constructor to use on a DbConnection that is already opened
        public DataContext(DbConnection existingConnection, bool contextOwnsConnection)
          : base(existingConnection, contextOwnsConnection)
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Acitvity>().MapToStoredProcedures();
            modelBuilder.Entity<Bond>().MapToStoredProcedures();
            modelBuilder.Entity<Booking>().MapToStoredProcedures();
            modelBuilder.Entity<CaseSummary>().MapToStoredProcedures();
            modelBuilder.Entity<CriminalHistory>().MapToStoredProcedures();
            modelBuilder.Entity<Hold>().MapToStoredProcedures();
            modelBuilder.Entity<Setting>().MapToStoredProcedures();
        }
    }
    /*
    CREATE TABLE bondsman.case_summary(
  Case_Number VARCHAR(64) NOT NULL,
 File_Date DATE NOT NULL,
  Case_Status VARCHAR(64) DEFAULT NULL,
 Offense VARCHAR(64) DEFAULT NULL,
Last_Instrument_Filed VARCHAR(64) DEFAULT NULL,
Disposition VARCHAR(128) DEFAULT NULL,
Completion_Date DATE DEFAULT NULL,
  Defendant_Status VARCHAR(64) DEFAULT NULL,
 Bond_Amount DECIMAL(10, 2) DEFAULT NULL,
Setting_Date DATE DEFAULT NULL,
  Def_Race_Sex VARCHAR(16) DEFAULT NULL,
 Def_Eyes VARCHAR(16) DEFAULT NULL,
Def_Skin VARCHAR(16) DEFAULT NULL,
Def_DOB DATE DEFAULT NULL,
  Def_US_Citizen VARCHAR(16) DEFAULT NULL,
 Def_Address VARCHAR(512) DEFAULT NULL,
Def_Markings VARCHAR(256) DEFAULT NULL,
Def_Height_Weight VARCHAR(32) DEFAULT NULL,
Def_Hair VARCHAR(16) DEFAULT NULL,
Def_Build VARCHAR(16) DEFAULT NULL,
Def_In_Custody VARCHAR(16) DEFAULT NULL,
Def_Place_Of_Birth VARCHAR(32) DEFAULT NULL,
CPJ_Current_Court VARCHAR(16) DEFAULT NULL,
CpjAddress VARCHAR(512) DEFAULT NULL,
CpjJudgeName VARCHAR(128) DEFAULT NULL,
CpjCourtType VARCHAR(32) DEFAULT NULL,
Case_GUID VARCHAR(32),
  PRIMARY KEY(Case_GUID)
)*/
    public partial class CaseSummary
    {
        public string CaseNumber { get; set; }
        public string CaseStatus { get; set; }
        public DateTime FileDate { get; set; }
        public string Status { get; set; }
        public string Offense { get; set; }
        public string LastInstrumentFiled { get; set; }
        public string Disposition { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string DefendantStatus { get; set; }
        public decimal? BondAmount { get; set; }
        public DateTime? SettingDate { get; set; }
        public string DefendantRaceSex { get; set; }
        public string DefendantEyes { get; set; }
        public string DefendantSkin { get; set; }
        public DateTime? DefendantDob { get; set; }
        public string DefendantUsCitizen { get; set; }
        public string DefendantAddress { get; set; }
        public string DefendantMarkings { get; set; }
        public string DefendantHeightWeight { get; set; }
        public string DefendantHair { get; set; }
        public string DefendantBuild { get; set; }
        public string DefendantInCustody { get; set; }
        public string DefendantPlaceOfBirth { get; set; }
        public string CpjCurrentCourt { get; set; }
        public string CpjAddress { get; set; }
        public string CpjJudgeName { get; set; }
        public string CpjCourtType { get; set; }
        public string CaseGuid { get; set; }
    }

    /*CREATE TABLE bondsman.bond(
  Case_Guid VARCHAR (32) NOT NULL,
  `Date` DATE NOT NULL,
  Type VARCHAR (64) NOT NULL,
  Description VARCHAR (512) DEFAULT NULL,
  SNU VARCHAR (32) DEFAULT NULL,
  INDEX bond_FK1 USING BTREE (Case_Guid),
  CONSTRAINT bond_FK1 FOREIGN KEY (Case_Guid)
  REFERENCES bondsman.case_summary (Case_GUID)
)*/
    public partial class Bond
    {
        public string CaseGuid { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Snu { get; set; }
    }
    /*CREATE TABLE bondsman.activity(
  Case_Guid VARCHAR (32) NOT NULL,
  `Date` DATE NOT NULL,
  Type VARCHAR (64) NOT NULL,
  Description VARCHAR (512) DEFAULT NULL,
  SNU VARCHAR (32) DEFAULT NULL,
  INDEX activity_FK1 USING BTREE (Case_Guid)
)*/
    public partial class Acitvity
    {
        public string CaseGuid { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Snu { get; set; }
    }

    /*CREATE TABLE bondsman.booking(
  Case_GUID VARCHAR (32) NOT NULL,
  Arrest_Date DATETIME DEFAULT NULL,
  Arrest_Location VARCHAR (64) DEFAULT NULL,
  Booking_Date DATETIME DEFAULT NULL,
  INDEX booking_FK1 USING BTREE (Case_GUID),
  CONSTRAINT booking_FK1 FOREIGN KEY (Case_GUID)
  REFERENCES bondsman.case_summary (Case_GUID)
)*/
    public partial class Booking
    {
        public string CaseGuid { get; set; }
        public DateTime? ArrestDate { get; set; }
        public string ArrestLocation { get; set; }
        public DateTime? BookingDate { get; set; }
    }

    /*CREATE TABLE bondsman.hold(
  Case_GUID VARCHAR (32) DEFAULT NULL,
  Agency_Placing_Hold VARCHAR (64) DEFAULT NULL,
  Agency_Name VARCHAR (64) DEFAULT NULL,
  Warrant_Number VARCHAR (64) DEFAULT NULL,
  Bond_Amount DECIMAL (10, 2) DEFAULT NULL,
  Offense VARCHAR (64) DEFAULT NULL,
  Placed_Date DATE DEFAULT NULL,
  Lifted_Date DATE DEFAULT NULL,
  INDEX hold_FK1 USING BTREE (Case_GUID),
  CONSTRAINT hold_FK1 FOREIGN KEY (Case_GUID)
  REFERENCES bondsman.case_summary (Case_GUID)
)*/
    public partial class Hold
    {
        public string CaseGuid { get; set; }
        public string AgencyPlacingHold { get; set; }
        public string AgencyName { get; set; }
        public string WarrantNumber { get; set; }
        public decimal? BondAmount { get; set; }
        public string Offense { get; set; }
        public DateTime? PlacedDate { get; set; }
        public DateTime? LiftedDate { get; set; }
    }

    /*CREATE TABLE bondsman.criminal_history(
  Case_GUID VARCHAR (32) NOT NULL,
  Case_Num_Status VARCHAR (128) NOT NULL,
  Defendant VARCHAR (128) DEFAULT NULL,
  Date_Filed DATE DEFAULT NULL,
  Date_Booked DATE DEFAULT NULL,
  Court VARCHAR (16) DEFAULT NULL,
  Defendant_Status VARCHAR (64) DEFAULT NULL,
  Disposition VARCHAR (128) DEFAULT NULL,
  Bond_Amount DECIMAL (10, 2) DEFAULT NULL,
  Offense VARCHAR (64) DEFAULT NULL,
  Next_Setting DATE DEFAULT NULL,
  INDEX criminal_history_FK1 USING BTREE (Case_GUID),
  CONSTRAINT criminal_history_FK1 FOREIGN KEY (Case_GUID)
  REFERENCES bondsman.case_summary (Case_GUID)
)*/

    public partial class CriminalHistory
    {
        public string CaseGuid { get; set; }
        public string CaseNumStatus { get; set; }
        public string Defendant { get; set; }
        public string DateFiledBooked { get; set; }
        public string Court { get; set; }
        public string DefendantStatus { get; set; }
        public string Disposition { get; set; }
        public decimal? BondAmount { get; set; }
        public string Offense { get; set; }
        public DateTime? NextSetting { get; set; }
    }

    /*CREATE TABLE bondsman.setting(
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
)*/

    public partial class Setting
    {
        public string CaseGuid { get; set; }
        public DateTime? Date { get; set; }
        public string Court { get; set; }
        public string PostJdgm { get; set; }
        public string DocketType { get; set; }
        public string Reason { get; set; }
        public string Results { get; set; }
        public string Defendant { get; set; }
        public DateTime? FutureDate { get; set; }
        public string Comments { get; set; }
        public string AttorneyAppearanceIndicator { get; set; }
    }
}
 
 
 
 