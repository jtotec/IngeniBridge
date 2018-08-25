﻿using IngeniBridge.Core.Diags;
using IngeniBridge.Core.MetaHelper;
using IngeniBridge.Core.StagingData;
using IngeniBridge.BuildUtils;
using MyCompanyDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IngeniBridge.Core.Util;
using IngeniBridge.Core.Storage;
using System.IO;
using OfficeOpenXml;
using IngeniBridge.Core.Inventory;
using log4net;
using System.Diagnostics;

namespace IngeniBridge.Samples.MyCompany
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger ( System.Reflection.MethodBase.GetCurrentMethod ().DeclaringType );
        static int Main ( string [ ] args )
        {
            int ret = 0;
            try
            {
                log4net.Config.XmlConfigurator.Configure ();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo ( Assembly.GetEntryAssembly ().Location );
                log.Info ( fvi.ProductName + " v" + fvi.FileVersion + " -- " + fvi.LegalCopyright );
                log.Info ( "Starting => " + Assembly.GetEntryAssembly ().GetName ().Name + " v" + Assembly.GetEntryAssembly ().GetName ().Version );
                log.Info ( "Data Model => " + Assembly.GetAssembly ( typeof ( ProductionSite ) ).GetName ().Name + " v" + Assembly.GetAssembly ( typeof ( ProductionSite ) ).GetName ().Version );
                #region init IngeniBridge
                UriBuilder uri = new UriBuilder ( Assembly.GetExecutingAssembly ().CodeBase );
                string path = Path.GetDirectoryName ( Uri.UnescapeDataString ( uri.Path ) );
                Assembly accessorasm = Assembly.LoadFile ( path + "\\IngeniBridge.StorageAccessor.InMemory.dll" );
                Core.Storage.StorageAccessor accessor = Core.Storage.StorageAccessor.InstantiateFromAccessorAssembly ( accessorasm );
                AssetExtension.StorageAccessor = accessor;
                TimedDataExtension.StorageAccessor = accessor;
                string fimastername = FileDater.SetFileNameDateTime ( "..\\..\\MasterAssetMyCompany.ibdb" );
                accessor.InitializeNewDB ( Assembly.GetAssembly ( typeof ( MyCompanyAsset ) ), fimastername );
                #endregion
                #region Init root asset
                MyCompanyRootAsset root = new MyCompanyRootAsset () { Code = "Root Asset", Label = "The root of my company's assests and measures" };
                accessor.SetRootAsset ( root );
                #endregion
                #region nomenclatures
                accessor.AddNomenclatureEntry ( new City () { Code = "PAR", Label = "Paris" } );
                accessor.AddNomenclatureEntry ( new City () { Code = "LIV", Label = "Livry-Gargan" } );
                accessor.AddNomenclatureEntry ( new TypeOfMeasure () { Code = "TMP", Label = "Temperature", Unit = "°C" } );
                accessor.AddNomenclatureEntry ( new TypeOfMeasure () { Code = "PRESS", Label = "Pressure", Unit = "bar" } );
                accessor.AddNomenclatureEntry ( new TypeOfMeasure () { Code = "ELEC", Label = "Electricity", Unit = "kw/h" } );
                accessor.AddNomenclatureEntry ( new TypeOfMeasure () { Code = "WT", Label = "Water throuput", Unit = "m3/h" } );
                accessor.AddNomenclatureEntry ( new TypeOfMeasure () { Code = "CL", Label = "Clorine", Unit = "mg/l" } );
                accessor.AddNomenclatureEntry ( new Sector () { Code = "W", Label = "West", City = accessor.FindNomenclatureEntry<City> ( "LIV" ) } );
                accessor.AddNomenclatureEntry ( new Sector () { Code = "S", Label = "South", City = accessor.FindNomenclatureEntry<City> ( "PAR" ) } );
                #endregion
                #region Influence zones
                // Here you see how to access an Excel file using the EPPlus package
                FileInfo fi = new FileInfo ( "..\\..\\Metadata content to consolidate.xlsx" );
                ExcelPackage xlConsolidate = new ExcelPackage ( fi );
                ExcelWorksheet wksZones = xlConsolidate.Workbook.Worksheets [ "Influence Zone" ];
                int line = 2;
                while ( wksZones.Cells [ line, 1 ].Value != null )
                {
                    InfluenceZone iz = new InfluenceZone () { Code = wksZones.Cells [ line, 1 ].Text, Label = wksZones.Cells [ line, 2 ].Text };
                    root.AddChildAsset ( iz );
                    line += 1;
                }
                xlConsolidate.Dispose ();
                #endregion
                #region assets
                ProductionSite siteParis = new ProductionSite () { Code = "Site of Paris", Label = "Site of Paris, production of water", Location = "Paris", Sector = accessor.FindNomenclatureEntry<Sector> ( "W" ) };
                siteParis.Zone = accessor.FindChildEntity<InfluenceZone> ( root.StorageUniqueID, "InfluenceZones", "Z1" ).Entity;
                root.AddChildAsset ( siteParis );
                ProductionSite siteLivry = new ProductionSite () { Code = "Site of Livry", Label = "Site of Livry-Gargan, quality of water", Location = "Livry-Gargan", Sector = accessor.FindNomenclatureEntry<Sector> ( "S" ) };
                siteLivry.Zone = accessor.FindChildEntity<InfluenceZone> ( root.StorageUniqueID, "InfluenceZones", "Z2" ).Entity;
                root.AddChildAsset ( siteLivry );
                ProductionSite siteLeRaincy = new ProductionSite () { Code = "Site of Le Raincy", Label = "Site of Le Raincy, Itercommunication", Location = "Le Raincy", Sector = accessor.FindNomenclatureEntry<Sector> ( "S" ) };
                siteLeRaincy.Zone = accessor.FindChildEntity<InfluenceZone> ( root.StorageUniqueID, "InfluenceZones", "Z2" ).Entity;
                root.AddChildAsset ( siteLeRaincy );
                GroupOfPumps grouppumps = new GroupOfPumps () { Code = "GP 001" };
                siteParis.AddChildAsset ( grouppumps );
                PressureSensor iot1 = new PressureSensor () { Code = "PS 001", TelephoneNumber = "0123456789" };
                grouppumps.AddChildAsset ( iot1 );
                WaterPump wp1 = new WaterPump () { Code = "WP 001" };
                grouppumps.AddChildAsset ( wp1 );
                WaterPump wp2 = new WaterPump () { Code = "WP 002" };
                grouppumps.AddChildAsset ( wp2 );
                ClorineInjector cl1 = new ClorineInjector () { Code = "CI 001" };
                siteLivry.AddChildAsset ( cl1 );
                MultiFunctionSensor iot2 = new MultiFunctionSensor () { Code = "MFS 001", TelephoneNumber = "1234567890" };
                cl1.AddChildAsset ( iot2 );
                WaterSwitcher ws1 = new WaterSwitcher () { Code = "WS 001" };
                siteLeRaincy.AddChildAsset ( ws1 );
                #endregion
                #region measures
                AcquiredMeasure am1 = new AcquiredMeasure () { Code = "M 001", TimeSeriesExternalReference = "EXTREF 001", tof = accessor.FindNomenclatureEntry<TypeOfMeasure> ( "WT" ), ConsolidationType = ConsolidationType.None };
                grouppumps.AddTimedData ( am1 );
                AcquiredMeasure am2 = new AcquiredMeasure () { Code = "M 002", TimeSeriesExternalReference = "EXTREF 002", tof = accessor.FindNomenclatureEntry<TypeOfMeasure> ( "ELEC" ), ConsolidationType = ConsolidationType.None };
                wp1.AddTimedData ( am2 );
                AcquiredMeasure am3 = new AcquiredMeasure () { Code = "M 003", TimeSeriesExternalReference = "EXTREF 003", tof = accessor.FindNomenclatureEntry<TypeOfMeasure> ( "ELEC" ), ConsolidationType = ConsolidationType.None };
                wp2.AddTimedData ( am3 );
                AcquiredMeasure am4 = new AcquiredMeasure () { Code = "M 004", TimeSeriesExternalReference = "EXTREF 004", tof = accessor.FindNomenclatureEntry<TypeOfMeasure> ( "PRESS" ), ConsolidationType = ConsolidationType.None };
                iot1.AddTimedData ( am4 );
                ComputedMeasure am5 = new ComputedMeasure () { Code = "M 005", TimeSeriesExternalReference = "EXTREF 005", tof = accessor.FindNomenclatureEntry<TypeOfMeasure> ( "ELEC" ), ConsolidationType = ConsolidationType.Average };
                siteParis.AddTimedData ( am5 );
                AcquiredMeasure am6 = new AcquiredMeasure () { Code = "M 006", TimeSeriesExternalReference = "EXTREF 006", tof = accessor.FindNomenclatureEntry<TypeOfMeasure> ( "CL" ), ConsolidationType = ConsolidationType.None };
                iot2.AddTimedData ( am6 );
                AcquiredMeasure am7 = new AcquiredMeasure () { Code = "M 007", TimeSeriesExternalReference = "EXTREF 007", tof = accessor.FindNomenclatureEntry<TypeOfMeasure> ( "PRESS" ), ConsolidationType = ConsolidationType.None };
                cl1.AddTimedData ( am7 );
                AcquiredMeasure am8 = new AcquiredMeasure () { Code = "M 008", TimeSeriesExternalReference = "EXTREF 008", tof = accessor.FindNomenclatureEntry<TypeOfMeasure> ( "ELEC" ), ConsolidationType = ConsolidationType.None };
                cl1.AddTimedData ( am8 );
                AcquiredMeasure am9 = new AcquiredMeasure () { Code = "M 009", TimeSeriesExternalReference = "EXTREF 010", tof = accessor.FindNomenclatureEntry<TypeOfMeasure> ( "CL" ), ConsolidationType = ConsolidationType.None };
                cl1.AddTimedData ( am9 );
                AcquiredMeasure am10 = new AcquiredMeasure () { Code = "M 010", TimeSeriesExternalReference = "EXTREF 009", tof = accessor.FindNomenclatureEntry<TypeOfMeasure> ( "ELEC" ), ConsolidationType = ConsolidationType.None };
                cl1.AddTimedData ( am10 );
                AcquiredMeasure am11 = new AcquiredMeasure () { Code = "M 011", TimeSeriesExternalReference = "EXTREF 011", tof = accessor.FindNomenclatureEntry<TypeOfMeasure> ( "PRESS" ), ConsolidationType = ConsolidationType.None };
                ws1.AddTimedData ( am11 );
                #endregion
                #region check and generation (generic script)
                TreeChecker tc = new TreeChecker ( accessor );
                tc.CheckTree ( true, message =>
                {
                    log.Info ( "error => " + message );
                } );
                int nbtotalassets = 0;
                int nbtotaldatas = 0;
                new Diagnostic ( accessor ).Diagnose ( ( assettype, nbassets, nbdatas ) =>
                {
                    log.Info ( assettype + " => " + nbassets.ToString () + ", nbvars => " + nbdatas.ToString () );
                    nbtotalassets += nbassets;
                    nbtotaldatas += nbdatas;
                }, ( nomenclature, nbentries ) =>
                {
                    log.Info ( nomenclature + ", nbentries => " + nbentries.ToString () );
                } );
                log.Info ( "nb total assets => " + nbtotalassets.ToString () );
                log.Info ( "nb total datas => " + nbtotaldatas.ToString () );
                accessor.CloseDB ();
                #endregion
            }
            catch ( Exception e )
            {
                log.Error ( "Exception => " + e.GetType () + " = " + e.Message );
                ret = 1;
            }
            return ( ret );
        }
    }
}
