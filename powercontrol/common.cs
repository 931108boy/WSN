using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//公用变数设定
namespace WindowsFormsApplication1
{
    public class common
    {
        //charging scheduling
        public static double request_threshold = 0.4;
        public static bool redraw = false;
        public static double default_pkt_leng = 10*1024*8; //10KByte
        public static int missed_task = 0;
        public static int done_task = 0;
        public static int request_task = 0;
        public static int sent_packet = 0;
        public static int recv_packet = 0;
        public static int dead_sentout = 0;
        public static int packet_id = 0; //for debug
        public static bool[] recved = new bool[100000];
        public static double target_ratio = 1;
        //for auto exec
        public static String source_dir;
        public static double total_move;
        public static double total_time_exec;
        public static int total_charged;
        public static int total_charging_generated;
        public static int total_late_charged;
        public static double total_life;
        public static int total_num_test;
        public static int start_n, end_n;
        public static int total_car_used;
        public static int total_sent;
        public static int total_recved;
        public static int total_deadsentout;
        public static int total_deadrecv;
        public static int total_lost;
        //end
        public static double min_next_charging_time = 0;

        public static int call_gene_count = 0;
        public static int total_gene_iterations = 0;

        public static bool debug_output = false;
        public static bool recalc_MRT = false;
        public static bool wait_for_charging = true;
        public static int modify_cnt = 2;
        public static StreamReader ifs;
        public static StreamWriter ofs;
        public static int current_time = 0;
        public static int last_exec_time = 0;
        public static double moving_distance = 0;
        public static int first_dead_time = -1;

        public static int saveNum = 0; //完成充電節點數
        public static int method_sel = 1;
        public static int window_size = 4;
        public static int max_iteration = 500;
        public static int population_size = 200;

        public static bool angle_sorting = false;
        public static bool EDF_gene = false;
        public static bool NJF_gene = false;
        public static bool useMaxCar = false;
        public static int calc_a = 0;
        public static int calc_b = 0;
        public static int calc_c = 1;

        public static bool fix_activate_condition = false;
        public static int assign_car_method = 0;
        public static int max_num_car = 10;
        public static int available_num_car = 1;
        public static int car_speed = 5;  // meter/sec
        public static double charging_speed = 5 * Math.Pow(10,9); // nJ/s
        public static int num_charger_per_car = 20; //几倍节点的原始电量 original_residual
        public static int fixed_bpr_max_task = 5; // <=0 時改回動態估算
        public static double Prate_change = 0.2; // 每次耗電速率變動檢查的機率
        public const int RATE_CHANGE_INTERVAL_SEC = 10000;
        public const double RATE_CHANGE_BAND = 0.125;

        public static bool car_no_return = false;
        public static int current_node = 0;
        public static bool dynamic_schedule = true;
        public static bool charging_time_include = true;
        public static int fix_waiting_time = 5000;
        public static double sensor_background_lifetime_sec = 100000.0; // 感測器滿電時的背景耗電壽命
        public const int CHARGING_VISUAL_HOLD_TICKS = 500;

        public const int MYINFINITE = Int32.MaxValue;
        public static double Origin_RESIDUAL = 500*Math.Pow(10,9);  // nJ
        //public static double Target_RESIDUAL = 9500; // for charging

        //end of charging scheduling
        public const int max_num_group = 10;
        public const int max_prange = 200;

        public const int SEND_HELLO = 0;
        public const int RECV_HELLO = 1;
        public const int SEND_UPDATE_GR = 2;
        public const int RECV_UPDATE_GR = 3;
        public const int SEND_DATA = 4;
        public const int RECV_DATA = 5;
        public const int FORWARD_DATA = 15;
        public const int BUILD_COST = 6;
        public const int GERR = 7;
        public const int CBR = 8;
        public const int LOST_EVENT = 9;

        public const int PKT_LOST = 0;
        public const int EVENT_RECV = 1;
        public const int DETECT_FAIL = 2;
        public const double TIME_UNIT = 0.01;
        public const int TX = 0;
        public const int RX = 0;
        public const double Eamp = 0.01; // 10pJ=0.01nJ, free space model
        //public const double Eamp = 0.0013*0.001; // 0.0013pJ=0.0000013nJ, multipath model
        public static Random rand = new Random();

        public static nodemap nmap;

        //functions
        public static double mydist(double x1, double y1, double x2, double y2)
        {
            double dist;
            dist = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            return dist;
        }
        public static int fact(int n)
        {
            int p = 1;
            for (int i = 2; i <= n; i++) p *= i;
            return p;
        }
        public static void getPermutation(int k, request_event[] data, int size)
        {
            //int size = data.Length;
            request_event temp;
            int pos;
            for (int j = 0; j < size; j++)
            {
                pos = k % (j + 1);
                temp = data[j];
                data[j] = data[pos];
                data[pos] = temp;
                k = (int)(k / (j + 1));
            }
        }

        public static int GetPoisson(double lambda)
        {
            return (lambda < 30.0) ? PoissonSmall(lambda) : PoissonLarge(lambda);
            //return PoissonSmall(lambda);
        }

        private static int PoissonSmall(double lambda)
        {
            // Algorithm due to Donald Knuth, 1969.
            double p = 1.0, L = Math.Exp(-lambda);
            Random rand = new Random(Guid.NewGuid().GetHashCode());//利用GUID产生随机数的种子，模拟真正的随机
            int k = 0;
            do
            {
                k++;
                p *= rand.NextDouble();
            }
            while (p > L);
            return k - 1;
        }

        private static int PoissonLarge(double lambda)
        {
            // "Rejection method PA" from "The Computer Generation of 
            // Poisson Random Variables" by A. C. Atkinson,
            // Journal of the Royal Statistical Society Series C 
            // (Applied Statistics) Vol. 28, No. 1. (1979)
            // The article is on pages 29-35. 
            // The algorithm given here is on page 32.

            double c = 0.767 - 3.36 / lambda;
            double beta = Math.PI / Math.Sqrt(3.0 * lambda);
            double alpha = beta * lambda;
            double k = Math.Log(c) - lambda - Math.Log(beta);
            Random rand = new Random(Guid.NewGuid().GetHashCode());//利用GUID产生随机数的种子，模拟真正的随机

            for (; ; )
            {
                double u = rand.NextDouble();
                double x = (alpha - Math.Log((1.0 - u) / u)) / beta;
                int n = (int)Math.Floor(x + 0.5);
                if (n < 0)
                    continue;
                double v = rand.NextDouble();
                double y = alpha - beta * x;
                double temp = 1.0 + Math.Exp(y);
                double lhs = y + Math.Log(v / (temp * temp));
                double rhs = k + n * Math.Log(lambda) - LogFactorial(n);
                if (lhs <= rhs)
                    return n;
            }
        }
        static double LogFactorial(int n)
        {
            if (n < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            else if (n > 254)
            {
                double x = n + 1;
                return (x - 0.5) * Math.Log(x) - x + 0.5 * Math.Log(2 * Math.PI) + 1.0 / (12.0 * x);
            }
            else
            {
                double[] lf =
                {
                    0.000000000000000,
                    0.000000000000000,
                    0.693147180559945,
                    1.791759469228055,
                    3.178053830347946,
                    4.787491742782046,
                    6.579251212010101,
                    8.525161361065415,
                    10.604602902745251,
                    12.801827480081469,
                    15.104412573075516,
                    17.502307845873887,
                    19.987214495661885,
                    22.552163853123421,
                    25.191221182738683,
                    27.899271383840894,
                    30.671860106080675,
                    33.505073450136891,
                    36.395445208033053,
                    39.339884187199495,
                    42.335616460753485,
                    45.380138898476908,
                    48.471181351835227,
                    51.606675567764377,
                    54.784729398112319,
                    58.003605222980518,
                    61.261701761002001,
                    64.557538627006323,
                    67.889743137181526,
                    71.257038967168000,
                    74.658236348830158,
                    78.092223553315307,
                    81.557959456115029,
                    85.054467017581516,
                    88.580827542197682,
                    92.136175603687079,
                    95.719694542143202,
                    99.330612454787428,
                    102.968198614513810,
                    106.631760260643450,
                    110.320639714757390,
                    114.034211781461690,
                    117.771881399745060,
                    121.533081515438640,
                    125.317271149356880,
                    129.123933639127240,
                    132.952575035616290,
                    136.802722637326350,
                    140.673923648234250,
                    144.565743946344900,
                    148.477766951773020,
                    152.409592584497350,
                    156.360836303078800,
                    160.331128216630930,
                    164.320112263195170,
                    168.327445448427650,
                    172.352797139162820,
                    176.395848406997370,
                    180.456291417543780,
                    184.533828861449510,
                    188.628173423671600,
                    192.739047287844900,
                    196.866181672889980,
                    201.009316399281570,
                    205.168199482641200,
                    209.342586752536820,
                    213.532241494563270,
                    217.736934113954250,
                    221.956441819130360,
                    226.190548323727570,
                    230.439043565776930,
                    234.701723442818260,
                    238.978389561834350,
                    243.268849002982730,
                    247.572914096186910,
                    251.890402209723190,
                    256.221135550009480,
                    260.564940971863220,
                    264.921649798552780,
                    269.291097651019810,
                    273.673124285693690,
                    278.067573440366120,
                    282.474292687630400,
                    286.893133295426990,
                    291.323950094270290,
                    295.766601350760600,
                    300.220948647014100,
                    304.686856765668720,
                    309.164193580146900,
                    313.652829949878990,
                    318.152639620209300,
                    322.663499126726210,
                    327.185287703775200,
                    331.717887196928470,
                    336.261181979198450,
                    340.815058870798960,
                    345.379407062266860,
                    349.954118040770250,
                    354.539085519440790,
                    359.134205369575340,
                    363.739375555563470,
                    368.354496072404690,
                    372.979468885689020,
                    377.614197873918670,
                    382.258588773060010,
                    386.912549123217560,
                    391.575988217329610,
                    396.248817051791490,
                    400.930948278915760,
                    405.622296161144900,
                    410.322776526937280,
                    415.032306728249580,
                    419.750805599544780,
                    424.478193418257090,
                    429.214391866651570,
                    433.959323995014870,
                    438.712914186121170,
                    443.475088120918940,
                    448.245772745384610,
                    453.024896238496130,
                    457.812387981278110,
                    462.608178526874890,
                    467.412199571608080,
                    472.224383926980520,
                    477.044665492585580,
                    481.872979229887900,
                    486.709261136839360,
                    491.553448223298010,
                    496.405478487217580,
                    501.265290891579240,
                    506.132825342034830,
                    511.008022665236070,
                    515.890824587822520,
                    520.781173716044240,
                    525.679013515995050,
                    530.584288294433580,
                    535.496943180169520,
                    540.416924105997740,
                    545.344177791154950,
                    550.278651724285620,
                    555.220294146894960,
                    560.169054037273100,
                    565.124881094874350,
                    570.087725725134190,
                    575.057539024710200,
                    580.034272767130800,
                    585.017879388839220,
                    590.008311975617860,
                    595.005524249382010,
                    600.009470555327430,
                    605.020105849423770,
                    610.037385686238740,
                    615.061266207084940,
                    620.091704128477430,
                    625.128656730891070,
                    630.172081847810200,
                    635.221937855059760,
                    640.278183660408100,
                    645.340778693435030,
                    650.409682895655240,
                    655.484856710889060,
                    660.566261075873510,
                    665.653857411105950,
                    670.747607611912710,
                    675.847474039736880,
                    680.953419513637530,
                    686.065407301994010,
                    691.183401114410800,
                    696.307365093814040,
                    701.437263808737160,
                    706.573062245787470,
                    711.714725802289990,
                    716.862220279103440,
                    722.015511873601330,
                    727.174567172815840,
                    732.339353146739310,
                    737.509837141777440,
                    742.685986874351220,
                    747.867770424643370,
                    753.055156230484160,
                    758.248113081374300,
                    763.446610112640200,
                    768.650616799717000,
                    773.860102952558460,
                    779.075038710167410,
                    784.295394535245690,
                    789.521141208958970,
                    794.752249825813460,
                    799.988691788643450,
                    805.230438803703120,
                    810.477462875863580,
                    815.729736303910160,
                    820.987231675937890,
                    826.249921864842800,
                    831.517780023906310,
                    836.790779582469900,
                    842.068894241700490,
                    847.352097970438420,
                    852.640365001133090,
                    857.933669825857460,
                    863.231987192405430,
                    868.535292100464630,
                    873.843559797865740,
                    879.156765776907600,
                    884.474885770751830,
                    889.797895749890240,
                    895.125771918679900,
                    900.458490711945270,
                    905.796028791646340,
                    911.138363043611210,
                    916.485470574328820,
                    921.837328707804890,
                    927.193914982476710,
                    932.555207148186240,
                    937.921183163208070,
                    943.291821191335660,
                    948.667099599019820,
                    954.046996952560450,
                    959.431492015349480,
                    964.820563745165940,
                    970.214191291518320,
                    975.612353993036210,
                    981.015031374908400,
                    986.422203146368590,
                    991.833849198223450,
                    997.249949600427840,
                    1002.670484599700300,
                    1008.095434617181700,
                    1013.524780246136200,
                    1018.958502249690200,
                    1024.396581558613400,
                    1029.838999269135500,
                    1035.285736640801600,
                    1040.736775094367400,
                    1046.192096209724900,
                    1051.651681723869200,
                    1057.115513528895000,
                    1062.583573670030100,
                    1068.055844343701400,
                    1073.532307895632800,
                    1079.012946818975000,
                    1084.497743752465600,
                    1089.986681478622400,
                    1095.479742921962700,
                    1100.976911147256000,
                    1106.478169357800900,
                    1111.983500893733000,
                    1117.492889230361000,
                    1123.006317976526100,
                    1128.523770872990800,
                    1134.045231790853000,
                    1139.570684729984800,
                    1145.100113817496100,
                    1150.633503306223700,
                    1156.170837573242400,
                };
                return lf[n];
            }
        }
    }

    public struct neighbor_info
    {
        public int id;
        public double dist;
        public bool used;
    }

    public class moving_data
    {
        public int target_id;
        public int event_id;
    }

    //genetic
    public class charge_info
    {
        public int destid_in_eventlist;
        public int car_no;
        public bool equals(charge_info x)
        {
            return (destid_in_eventlist == x.destid_in_eventlist) && (car_no == x.car_no);
        }
    }

    public class event_block
    {
        public int num_event = 0;
        public List<int> event_list = new List<int>();
        public event_block(int eventid)
        {
            num_event = 1;
            event_list.Add(eventid);
        }
        public void add_member(int memberid)
        {
            event_list.Add(memberid);
            num_event++;
        }
    }
    public class group_info
    {
        public double cx, cy;
        public int num_member;
        public List<int> member_list;
        public double max_dist;
        public group_info()
        {
            num_member = 0;
            max_dist = 0;
            member_list = new List<int>();
        }
    }
    public class for_sorting_distance
    {
        public int nid; //node id
        public double dist;
        public int carno, oldcarno;
        public int event_list_order;
    }

    public struct child_info
    {
        int id;
        uint set_time;
    }

}
