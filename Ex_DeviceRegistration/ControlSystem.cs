//-----------------------------------------------------------------------
// <copyright file="ControlSystem.cs" company="Crestron">
//     Copyright (c) Crestron Electronics. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crestron.SimplSharp;                       // For Basic SIMPL# Classes
using Crestron.SimplSharp.CrestronIO;            // For Directory
using Crestron.SimplSharpPro;                    // For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;     // For Threading
using Crestron.SimplSharpPro.DeviceSupport;      // For Generic Device Support
using Crestron.SimplSharpPro.Diagnostics;        // For System Monitor Access
using Crestron.SimplSharpPro.UI;                 // For xPanelForSmartGraphics

namespace Ex_DeviceRegistration
{
    /* Notes
     * Please the task list (View -> Task List in VS2019) to get a list of all the different tasks.
     * Read the Student Guide to get more detailed information on functionality
     */

    /// <summary>
    /// ControlSystem class that inherits from CrestronControlSystem
    /// </summary>
    public class ControlSystem : CrestronControlSystem
    {
        /// <summary>
        /// Used for logging information to error log
        /// </summary>
        private const string LogHeader = "[Device] ";

        /// <summary>
        /// Touchpanel used throughout this exercise
        /// Could also be a Tsw or any other SmartGraphics enabled touchpanel
        /// </summary>
        private XpanelForSmartGraphics tp01;
        private XpanelForSmartGraphics tp02;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlSystem" /> class.
        /// Use the constructor to:
        /// * Initialize the maximum number of threads (max = 400)
        /// * Register devices
        /// * Register event handlers
        /// * Add Console Commands
        /// Please be aware that the constructor needs to exit quickly; if it doesn't
        /// exit in time, the SIMPL#Pro program will exit.
        /// You cannot send / receive data in the constructor
        /// </summary>
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                // Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(this.ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(this.ControlSystem_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(this.ControlSystem_ControllerEthernetEventHandler);

                if (this.SupportsEthernet)
                {
                    // TODO: Level 1,2,3. Create new XpanelForSmartGraphics on IPID 03
                    // You can use the XpanelForSmartGraphics defined on line 40

                    tp01 = new XpanelForSmartGraphics(0x03, this);

                    // Example. This is how you can subscribe to the SigChange event handler
                    this.tp01.SigChange += Tp01_SigChange;

                    // TODO: Level 1,2,3. Register the OnlineStatusChange event handler yourself here
                    this.tp01.OnlineStatusChange += Xpanel_OnlineStatusChange;

                    // TODO: Level 1,2,3. After all the event handlers are done, register the touchpanel. Try to check for success
                    tp01.Register();

                    // We have left this in to show how to use Smart Objects
                    // However we are not using them for this exercise
                    // You should only proceed with this if the touchpanel registration was successful!
                    string sgdPath = string.Format(@"{0}/XPanel_Masters2020.sgd", Directory.GetApplicationDirectory());
                    this.tp01.LoadSmartObjects(sgdPath);
                    ErrorLog.Error(string.Format(LogHeader + "Loaded {0} SmartObjects", this.tp01.SmartObjects.Count));
                    foreach (KeyValuePair<uint, SmartObject> smartObject in this.tp01.SmartObjects)
                    {
                        smartObject.Value.SigChange += new SmartObjectSigChangeEventHandler(this.Xpanel_SO_SigChange);
                    }
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error(string.Format(LogHeader + "Error in the constructor: {0}", e.Message));
            }
        }

        /// <summary>
        /// Eventhandler for boolean/ushort/string sigs
        /// </summary>
        /// <param name="currentDevice">The device that triggered the event</param>
        /// <param name="args">Contains the SigType, Sig.Number and Sig.Value and more</param>
        public void Tp01_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            // You want to check which type of Sig it is first.
            // This can be either Boolean, UShort or String
            switch (args.Sig.Type)
            {
                // Handle the specific Sig type individually
                case eSigType.Bool:
                    // args.Sig.Number is your join number for this specific Sig type
                    switch (args.Sig.Number)
                    {
                        // Example only to show to handle digital join 1
                        // You can remove this if wanted
                        case 1:
                                ErrorLog.Notice("Digital Join 1 was triggered!");
                            break;

                        // TODO: Level1. Implement digital join 12 to show/hide Hello World string on serial join 11
                        case 12:
                            ErrorLog.Notice("Digital Join 12 was triggered!");
                            tp01.StringInput[11].StringValue = args.Sig.BoolValue ? "Hello World!" : string.Empty;
                            break;


                        // TODO: Level2. Implement toggle button logic on digital join 21
                        case 21:
                            if (args.Sig.BoolValue == true && tp01.BooleanInput[21].BoolValue == false)
                            {
                                tp01.BooleanInput[21].BoolValue = true;
                                tp01.StringInput[21].StringValue = "Hello World";
                            }
                            else if (args.Sig.BoolValue == true && tp01.BooleanInput[21].BoolValue == true)
                            {
                                tp01.BooleanInput[21].BoolValue = false;
                                tp01.StringInput[21].StringValue = "";
                            }
                            break;

                        // TODO: Level2. Implement interlock button logic on digital joins 22,23 and 24
                        // TODO: Level2. Implement interlock clear button on digital join 25


                        case 22:
                        case 23:
                        case 24:
                        case 25:
                            currentDevice.BooleanInput[22].BoolValue = false;
                            currentDevice.BooleanInput[23].BoolValue = false;
                            currentDevice.BooleanInput[24].BoolValue = false;
                            currentDevice.BooleanInput[args.Sig.Number].BoolValue = true;

                            var interlockStates = new[] {"Interlock 1", "Interlock 2", "Interlock 3", string.Empty};

                            currentDevice.StringInput[21].StringValue = interlockStates[args.Sig.Number - 22];
                            break;



                        // TODO: Level3. Implement additional TP registration button on digital join 31
                        case 31:
                            if (currentDevice.ID == tp01.ID && args.Sig.Type == eSigType.Bool && args.Sig.Number == 31)
                            {

                                if (currentDevice.BooleanOutput[31].BoolValue == true)
                                {

                                    if (tp02.Registered == true) tp02.UnRegister();

                                    else

                                        tp02.Register();
                                }
                            }
                            break;
            }

            break;
                case eSigType.UShort:
                // TODO: Level3. Implement slider logic for analog join 31.
                    double value = currentDevice.UShortOutput[31].UShortValue * 100 / 65535;
                    currentDevice.UShortInput[31].UShortValue = currentDevice.UShortOutput[31].UShortValue;
                    currentDevice.UShortInput[32].UShortValue = Convert.ToUInt16(value);
                    currentDevice.UShortInput[33].UShortValue = Convert.ToUInt16(
                       value < 1 ? 0 :
                       value < 33 ? 1 :
                       value < 66 ? 2 :
                       3
                    );
                    break;
                   

                case eSigType.String:
                    break;
            }
        }

        /// <summary>
        /// Online/Ofline event handler for Xpanel
        /// </summary>
        /// <param name="currentDevice">The device that triggered the event</param>
        /// <param name="args">Contains DeviceOnline for status feedback</param>
        public void Xpanel_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            // TODO: Level1. Implement Xpanel online/offline feedback on digital join 11
            if (args.DeviceOnLine) tp01.BooleanInput[11].BoolValue = true;

        }

        /// <summary>
        /// Specific event handler for Smart Objects (not used in this exercise)
        /// </summary>
        /// <param name="currentDevice">The device that triggered the event</param>
        /// <param name="args">Contains args.Sig.Type, args.Sig.Name, args.SmartObjectArgs.ID and more</param>
        public void Xpanel_SO_SigChange(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            // Not implemented. Just added as a reference for how to subscribe to Smart Object events.
        }

        /// <summary>
        /// InitializeSystem - this method gets called after the constructor 
        /// has finished. 
        /// Use InitializeSystem to:
        /// * Start threads
        /// * Configure ports, such as serial and verisports
        /// * Start and initialize socket connections
        /// Send initial device configurations
        /// Please be aware that InitializeSystem needs to exit quickly also; 
        /// if it doesn't exit in time, the SIMPL#Pro program will exit.
        /// </summary>
        public override void InitializeSystem()
        {
        }

        /// <summary>
        /// Event Handler for Ethernet events: Link Up and Link Down. 
        /// Use these events to close / re-open sockets, etc. 
        /// </summary>
        /// <param name="ethernetEventArgs">This parameter holds the values 
        /// such as whether it's a Link Up or Link Down event. It will also indicate 
        /// wich Ethernet adapter this event belongs to.
        /// </param>
        public void ControlSystem_ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {
                // Determine the event type Link Up or Link Down
                case eEthernetEventType.LinkDown:
                    // Next need to determine which adapter the event is for. 
                    // LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                    }

                    break;
                case eEthernetEventType.LinkUp:
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                    }

                    break;
            }
        }

        /// <summary>
        /// Event Handler for Programmatic events: Stop, Pause, Resume.
        /// Use this event to clean up when a program is stopping, pausing, and resuming.
        /// This event only applies to this SIMPL#Pro program, it doesn't receive events
        /// for other programs stopping
        /// </summary>
        /// <param name="programStatusEventType">Stop, resume or pause</param>
        public void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case eProgramStatusEventType.Paused:
                    // ErrorLog.Notice(string.Format("Program Paused"));
                    break;
                case eProgramStatusEventType.Resumed:
                    // ErrorLog.Notice(string.Format("Program Resumed"));
                    break;
                case eProgramStatusEventType.Stopping:
                    // ErrorLog.Notice(string.Format("Program Stopping"));
                    break;
            }
        }

        /// <summary>
        /// Event Handler for system events, Disk Inserted/Ejected, and Reboot
        /// Use this event to clean up when someone types in reboot, or when your SD /USB
        /// removable media is ejected / re-inserted.
        /// </summary>
        /// <param name="systemEventType">Inserted, Removed, Rebooting</param>
        public void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case eSystemEventType.DiskInserted:
                    // Removable media was detected on the system
                    break;
                case eSystemEventType.DiskRemoved:
                    // Removable media was detached from the system
                    break;
                case eSystemEventType.Rebooting:
                    // The system is rebooting. 
                    // Very limited time to preform clean up and save any settings to disk.
                    break;
            }
        }
    }
}
