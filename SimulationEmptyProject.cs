
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Ccr.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using W3C.Soap;
using Microsoft.Robotics.Simulation.Engine;
using Microsoft.Robotics.PhysicalModel;
using Microsoft.Robotics.Simulation.Physics;

using submgr = Microsoft.Dss.Services.SubscriptionManager;
using engine = Microsoft.Robotics.Simulation.Engine.Proxy;
using drive = Microsoft.Robotics.Services.Simulation.Drive.Proxy;

using Microsoft.Robotics.Simulation;
using xna = Microsoft.Xna.Framework;
using xnagrfx = Microsoft.Xna.Framework.Graphics;
using xnaprof = Microsoft.Robotics.Simulation.MeshLoader;


namespace Robotics.SimulationEmptyProject
{
    [Contract(Contract.Identifier)]
    [DisplayName("(User) Simulation Empty Project")]
    [Description("Minimal base template for developing a service that uses the simulation engine")]
    class SimulationEmptyProjectService : DsspServiceBase
    {
        /// <summary>
        /// 服务状态
        /// </summary>
        [ServiceState]
        SimulationEmptyProjectState _state = new SimulationEmptyProjectState();

        //模拟引擎 Port
        [Partner("Engine",
            Contract = engine.Contract.Identifier,
            CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        private engine.SimulationEnginePort _engineStub =
            new engine.SimulationEnginePort();

        /// <summary>
        /// 主要服务 port
        /// </summary>
        [ServicePort("/SimulationEmptyProject", AllowMultipleInstances = true)]
        SimulationEmptyProjectOperations _mainPort = new SimulationEmptyProjectOperations();

        [SubscriptionManagerPartner]
        submgr.SubscriptionManagerPort _submgrPort = new submgr.SubscriptionManagerPort();

        /// <summary>
        /// 仿真引擎 partner
        /// </summary>
        [Partner("SimulationEngine", Contract = engine.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        engine.SimulationEnginePort _simulationEnginePort = new engine.SimulationEnginePort();
        engine.SimulationEnginePort _simulationEngineNotify = new engine.SimulationEnginePort();

        /// <summary>
        /// Service constructor
        /// </summary>
        public SimulationEmptyProjectService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        /// <summary>
        /// 服务启动
        /// </summary>
        protected override void Start()
        {
            
            // 
            // 添加服务初始化
            // 
            base.Start();

            // 添加实体或者物体
            SetupCamera();
            PopulateWorld();

         

        }
        void PopulateWorld()
        {
            AddSky();
            AddGround();
           // AddGrassFloor();
            AddBoeBot(new Vector3(-8.0f, 0.1f, -20.0f));            
 
        }

        void AddBoeBot(Vector3 pos)
        {
            BoeBot boeBot = new BoeBot(pos);
            boeBot.State.Name = "SimulatedBoeBot";
            boeBot.State.Pose.Position = new Vector3(-12.05f, 0.020f,-15.0f);
            boeBot.MeshScale = new Vector3(4.0f,4.0f,4.0f);

            // Start simulated Boe-Bot Drive service
            CreateService(
               drive.Contract.Identifier,
                 Microsoft.Robotics.Simulation.Partners.CreateEntityPartner(
                  "http://localhost/" + boeBot.State.Name));

            
            SimulationEngine.GlobalInstancePort.Insert(boeBot);
        }
      
        //草坪
        void AddGrassFloor()
        {
            int XDimCount = 15;
            int YDimCount = 30;
       
            Vector3 initialPos = new Vector3(1, 0, -20);
            float mazeheight = 1f;
            float blocksize = 3.0f;
            MaterialProperties bouncyMaterial = new MaterialProperties("Bouncy", 1.0f, 0.5f, 0.6f);
            MaterialProperties SlickMaterial = new MaterialProperties("Slick", 1.0f, 0.0f, 0.0f);

            BoxShape[] boxShapes = new BoxShape[XDimCount * YDimCount];
          

            float coe = 0.1f;
            for (int x = 0; x < XDimCount; x++)
            {
                for (int y = 0; y < YDimCount; y++)
                {
                    boxShapes[y * XDimCount + x] = new BoxShape(
                        new BoxShapeProperties(
                        0.001f, // 重量
                        new Pose(new Vector3((initialPos.X  + x * blocksize)* coe, initialPos.Y + mazeheight / 2, (initialPos.Z * coe - y * blocksize)* coe)), 
                        new Vector3(blocksize* coe, mazeheight* coe, blocksize* coe)));
                    boxShapes[y * XDimCount + x].State.Material = new MaterialProperties("grass.jpg",
                  0.2f, // restitution
                  0.5f, // dynamic friction
                  0.5f);// static friction

                    boxShapes[y * XDimCount + x].State.TextureFileName = "grass.jpg";
                }
            }

                     
            MultiShapeEntity GrassFloor = new MultiShapeEntity(boxShapes, null);
            GrassFloor.State.Pose.Position = new Vector3(-13.534f,0,-15.664f);
            GrassFloor.State.Name = "GrassFloor";
            SimulationEngine.GlobalInstancePort.Insert(GrassFloor);

           
        }

        /// <summary>
        /// 设定一个镜头
        /// </summary>
        private void SetupCamera()
        {
            CameraView view = new CameraView();
            

            view.EyePosition = new Vector3(-17.65f, 1.63f, -20.29f);
            view.LookAtPoint = new Vector3(-16.68f, 1.38f, -20.18f);

            SimulationEngine.GlobalInstancePort.Update(view);
        }

        /// <summary>
        /// 加上天空 , 太阳
        /// </summary>
        void AddSky()
        {
            //加上天空
            SkyDomeEntity sky = new SkyDomeEntity("skydome.dds", "sky_diff.dds");
            SimulationEngine.GlobalInstancePort.Insert(sky);

            //用平行光源模拟太阳
            LightSourceEntity sun = new LightSourceEntity();
            sun.State.Name = "Sun";
            sun.Type = LightSourceEntityType.Directional;
            sun.Color = new Vector4(0.8f, 0.8f, 0.8f, 1);
            sun.Direction = new Vector3(0.5f, -.75f, 0.5f);
            SimulationEngine.GlobalInstancePort.Insert(sun);
        }

        /// <summary>
        /// 加上地面
        /// </summary>
        void AddGround()
        {
            // 产生一个宽广的地面
            HeightFieldEntity ground = new HeightFieldEntity(
                "simple ground", 
                "03RamieSc.dds", 
                new MaterialProperties("ground",
                    0.2f, // restitution
                    0.5f, // dynamic friction
                    0.5f) // static friction 
                );
            SimulationEngine.GlobalInstancePort.Insert(ground);
        }

        /// <summary>
        /// 订阅信息处理
        /// </summary>
        /// <param name="subscribe">the subscribe request</param>
        [ServiceHandler]
        public void SubscribeHandler(Subscribe subscribe)
        {
            SubscribeHelper(_submgrPort, subscribe.Body, subscribe.ResponsePort);
        }
    }

    [DataContract]
    public class BoeBot : DifferentialDriveEntity
    {
        Port<EntityContactNotification> _notifications = new Port<EntityContactNotification>();

        /// <summary>
        /// Default constructor, used for creating the entity from an XML description
        /// </summary>
        public BoeBot() { }

        /// <summary>
        /// Custom constructor for building model from hardcoded values. Used to create entity programmatically
        /// </summary>
        /// <param name="initialPos"></param>
        public BoeBot(Vector3 initialPos)
        {
            MASS = 0.454f; //质量
            // the default settings approximate the BoeBot chassis
            CHASSIS_DIMENSIONS = new Vector3(0.09f, //宽
                                                0.09f,  //高
                                                0.13f); //长
            FRONT_WHEEL_MASS = 0.01f;
            CHASSIS_CLEARANCE = 0.015f;
            FRONT_WHEEL_RADIUS = 0.025f;
            CASTER_WHEEL_RADIUS = 0.0125f;
            FRONT_WHEEL_WIDTH = 0.01f;
            CASTER_WHEEL_WIDTH = 0.008f; //not currently used, but dim is accurate
            FRONT_AXLE_DEPTH_OFFSET = 0.01f; // distance of the axle from the center of robot

            base.State.Name = "BoeBot";
            base.State.MassDensity.Mass = MASS;
            base.State.Pose.Position = initialPos;

           
            BoxShapeProperties motorBaseDesc = new BoxShapeProperties("BoeBot Body", MASS,
                new Pose(new Vector3(
                0, // Chassis center is also the robot center, so use zero for the X axis offset
                CHASSIS_CLEARANCE + CHASSIS_DIMENSIONS.Y / 2, // chassis is off the ground and its center is DIM.Y/2 above the clearance
                0.03f)), // minor offset in the z/depth axis
                CHASSIS_DIMENSIONS);

            motorBaseDesc.Material = new MaterialProperties("high friction", 0.0f, 1.0f, 20.0f);
            motorBaseDesc.Name = "Chassis";
            ChassisShape = new BoxShape(motorBaseDesc);

            // rear wheel is also called the caster
            CASTER_WHEEL_POSITION = new Vector3(0, // center of chassis
                CASTER_WHEEL_RADIUS, // distance from ground
                CHASSIS_DIMENSIONS.Z / 2); // at the rear of the robot

            RIGHT_FRONT_WHEEL_POSITION = new Vector3(
                +CHASSIS_DIMENSIONS.X / 2,// left of center
                FRONT_WHEEL_RADIUS,// distance from ground of axle
                FRONT_AXLE_DEPTH_OFFSET); // distance from center, on the z-axis

            LEFT_FRONT_WHEEL_POSITION = new Vector3(
                -CHASSIS_DIMENSIONS.X / 2,// right of center
                FRONT_WHEEL_RADIUS,// distance from ground of axle
                FRONT_AXLE_DEPTH_OFFSET); // distance from center, on the z-axis

            MotorTorqueScaling = 30;

            // specify a default mesh 
            State.Assets.Mesh = "boe-bot.bos";

        }
    }
}


