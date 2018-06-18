using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using MySql.Data.Entity;

namespace BondsmenScrapper.Data
{
    // Code-Based Configuration and Dependency resolution
    //[DbConfigurationType(typeof(MySqlEFConfiguration))]
    public partial class DataContext : DbContext
    {
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Bond> Bonds { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<CaseSummary> CaseSummaries { get; set; }
        public DbSet<CriminalHistory> CriminalHistories { get; set; }
        public DbSet<Hold> Holds { get; set; }
        public DbSet<Setting> Settings { get; set; }

        public DataContext()
        {

        }

        // Constructor to use on a DbConnection that is already opened
        public DataContext(DbConnection existingConnection, bool contextOwnsConnection)
          : base(existingConnection, contextOwnsConnection)
        {

        }
    }
    
    public partial class CaseSummary
    {
        public int Id { get; set; }
        public string CaseNumber { get; set; }
        public string CaseStatus { get; set; }
        public DateTime FileDate { get; set; }
        public string Offense { get; set; }
        public string LastInstrumentFiled { get; set; }
        public string Disposition { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string DefendantStatus { get; set; }
        public string BondAmount { get; set; }
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
    
    public partial class Bond
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Snu { get; set; }
    }

    public partial class Activity
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string SnuCfi { get; set; }
    }
    
    public partial class Booking
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public DateTime? ArrestDate { get; set; }
        public string ArrestLocation { get; set; }
        public DateTime? BookingDate { get; set; }
    }
    
    public partial class Hold
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public string AgencyPlacingHold { get; set; }
        public string AgencyName { get; set; }
        public string WarrantNumber { get; set; }
        public string BondAmount { get; set; }
        public string Offense { get; set; }
        public DateTime? PlacedDate { get; set; }
        public DateTime? LiftedDate { get; set; }
    }
    
    public partial class CriminalHistory
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public string CaseNumStatus { get; set; }
        public string Defendant { get; set; }
        public string DateFiledBooked { get; set; }
        public string Court { get; set; }
        public string DefendantStatus { get; set; }
        public string Disposition { get; set; }
        public string BondAmount { get; set; }
        public string Offense { get; set; }
        public DateTime? NextSetting { get; set; }
    }
    
    public partial class Setting
    {
        public int Id { get; set; }
        public int CaseId { get; set; }
        public DateTime Date { get; set; }
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
 
 
 
 