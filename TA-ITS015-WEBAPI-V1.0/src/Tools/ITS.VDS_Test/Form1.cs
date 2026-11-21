using System.Configuration;
using Furion;
using Modules.TMS.Controllers.Zone.Dto;
using Modules.TMS.Core.Entities;
using Modules.TMS.Infrastructure;
using Shared.DTO.Enums;
using SqlSugar;

namespace ITS.VDS_Test
{
    public partial class Form1 : Form
    {
        BaseRepository<TmsTrafficData> _tmsTrafficDataRep;
        private List<TmsEquipment> listEquip;
        private List<TmsZone> listZone;

        private SqlSugarClient Db;

        public Form1()
        {
            InitializeComponent();

            //_tmsTrafficDataRep = new BaseRepository<tmsTrafficData>();

        }


        private void fn_Init()
        {
            try
            {
                var connection = ConfigurationManager.AppSettings["Connection"];
                MessageBox.Show(connection);
                Db = new SqlSugarClient(new ConnectionConfig()
                {
                    ConnectionString = connection,
                    DbType = DbType.SqlServer,
                    IsAutoCloseConnection = true
                },
                    db => { });

                fn_LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void fn_LoadData()
        {
            var camName = textBox2.Text?.Split('|');
            listEquip = Db.Queryable<TmsEquipment>().Where(l => camName.Any(cus => l.Name.Contains(cus))).ToList();
            listZone = Db.Queryable<TmsZone>().ToList();


        }

        private void button1_Click(object sender, EventArgs e)
        {
            fn_Init();
            var time = 1500;
            Int32.TryParse(textBox1.Text, out time);
            tmrSend?.Stop();
            tmrSend = new System.Windows.Forms.Timer();
            tmrSend.Interval = time;
            tmrSend.Tick += (s, ev) =>
            {
                fn_InsertData();
            };
            tmrSend.Start();


        }


        private void fn_InsertData()
        {
            var randomEquip = new Random().Next(0, listEquip.Count);
            var equip = listEquip[randomEquip];
            
            var type = new string[] { txtShort.Text, txtMedium.Text, txtLong.Text };
            var randType = new Random().Next(0, type.Length);
            var zone = HandleAsync(new TmsZonePositionInput()
            {
                KmNumber = equip.KmNumber,
                MetNumber = equip.MetNumber,
                DirectionId = equip.DirectionId + "",
                LaneId = equip.DirectionId + "",
                //ActiveDirection = equip.ActiveDirection + ""
            }).Result;

            Db.Insertable(new TmsTrafficData()
            {
                Length = new Random().Next(6500, 15000),
                Width = new Random().Next(1300, 4000),
                Height = new Random().Next(2300, 4500),
                Speed = new Random().Next(30, 120),
                DetectTime = DateTime.Now,
                Lane = new Random().Next(1, 2) + "",
                Location = "A1",
                IpAddress = equip.Ip,
                AlarmHeight = false,
                AlarmLength = false,
                AlarmSpeed = false,
                AlarmWidth = false,
                Direction = equip.DirectionId + "",
                EquipmentId = equip.ID,
                LicensePlate = new Random().Next(49, 55) + "NA" + new Random().Next(12345, 99999),
                Type = type[randType],
                ZoneId = zone.FirstOrDefault()?.ID,
                ID = Guid.NewGuid().ToString(),
                Code = equip.Code



            }).ExecuteCommandAsync();
        }


        public async Task<List<TmsZone>> HandleAsync(TmsZonePositionInput command)
        {

            // Chuyển đổi vị trí từ đơn vị km sang đơn vị met
            int incidentPos = (command.KmNumber ?? 0) * 1000 + (command.MetNumber ?? 0);

            var zList = listZone
                .Where(
                    z => z.LaneId != null && z.LaneId.ToString() == command.LaneId)
                .Where(z =>
                    (((z.FromKmNumber ?? 0) * 1000 + (z.FromMetNumber ?? 0) <= incidentPos)
                    &&
                    ((z.ToKmNumber ?? 0) * 1000 + (z.ToMetNumber ?? 0) > incidentPos))
                    ||
                    (((z.FromKmNumber ?? 0) * 1000 + (z.FromMetNumber ?? 0) > incidentPos)
                     &&
                     ((z.ToKmNumber ?? 0) * 1000 + (z.ToMetNumber ?? 0) <= incidentPos))
                )
                .Where(z => z.Status == BaseEnums.StatusEnum.Enable && z.IsDelete == null)
               .ToList();

            return zList;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            fn_LoadData();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            tmrSend?.Stop();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            fn_Init();
            fn_InsertData();
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }
    }
}
