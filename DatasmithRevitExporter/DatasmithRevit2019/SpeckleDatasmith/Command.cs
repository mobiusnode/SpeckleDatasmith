using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

using SpeckleCore;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using DatasmithRevitExporter;

namespace SpeckleDatasmith
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public SpeckleApiClient Client;
        private string RestApi;
        private string StreamId;
        public Dictionary<string, SpeckleObject> ObjectCache = new Dictionary<string, SpeckleObject>();
        private System.Timers.Timer MetadataSender, DataSender;

        private string BucketName;
        private List<Layer> BucketLayers = new List<Layer>();
        private List<object> BucketObjects = new List<object>();

        public bool IsSendingUpdate = false;
        private string NickName = "nickname";

        public FDatasmithFacadeScene DSscene;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (Client == null)
            {
                Account account = null;
                try
                {
                    RestApi = account.RestApi;
                    Client = new SpeckleApiClient(account.RestApi);
                    Client.IntializeSender(account.Token, "docName", "Revit_Datasmith", "").ContinueWith(task =>
                    { });
                }
                catch( Exception err)
                {
                }

                if (account == null)
                {
                    var signInWindow = new SpecklePopup.SignInWindow(true);

                    signInWindow.ShowDialog();

                    if (signInWindow.AccountListBox.SelectedIndex != -1)
                    {
                        account = signInWindow.accounts[signInWindow.AccountListBox.SelectedIndex];
                        RestApi = account.RestApi;
                        Client = new SpeckleApiClient(account.RestApi);
                        Client.IntializeSender(account.Token, "docName", "Revit_Datasmith", "").ContinueWith(task =>
                        { });
                    }
                    else
                    {
                        return Result.Cancelled;
                    }

                }
            }
            else
            {
            }

            Client.OnReady += (sender, e) =>
            {
                StreamId = Client.StreamId;
            };

            MetadataSender = new System.Timers.Timer(1000) { AutoReset = false, Enabled = false };
            MetadataSender.Elapsed += MetadataSender_Elapsed;

            DataSender = new System.Timers.Timer(2000) { AutoReset = false, Enabled = false };
            DataSender.Elapsed += DataSender_Elapsed;

            ObjectCache = new Dictionary<string, SpeckleObject>();


            // Original datasmith form. Need to remove form.
            DatasmithRevitCommand dmrc = new DatasmithRevitCommand();
            dmrc.SpeckleCommand = this;
            dmrc.Execute(commandData, ref message, elements);



            //ForceUpdateData();


            return Result.Succeeded;
        }


        public void UpdateData()
        {
            BucketName = NickName;
            BucketLayers = GetLayers();
            BucketObjects = GetData();

            DataSender.Start();
        }

        public void ForceUpdateData()
        {
            BucketName = NickName;
            BucketLayers = GetLayers();
            BucketObjects = GetData();

            SendUpdate();
        }

        public void UpdateMetadata()
        {
            //if (DocumentIsClosing)
            //{
            //    return;
            //}

            BucketName = NickName;
            BucketLayers = GetLayers();

            MetadataSender.Start();
        }

        private void DataSender_Elapsed(object sender, ElapsedEventArgs e)
        {
            //if (!ManualMode)
            //{
                SendUpdate();
            //}
        }

        private void MetadataSender_Elapsed(object sender, ElapsedEventArgs e)
        {
            //if (ManualMode)
            //{
            //    return;
            //}
            // we do not need to enque another metadata sending event as the data update superseeds the metadata one.
            if (DataSender.Enabled) { return; };
            SpeckleStream updateStream = new SpeckleStream()
            {
                Name = BucketName,
                Layers = BucketLayers
            };

            var updateResult = Client.StreamUpdateAsync(Client.StreamId, updateStream).Result;

            //Log += updateResult.Message;
            Client.BroadcastMessage("stream", Client.StreamId, new { eventType = "update-meta" });
        }

        private void SendUpdate()
        {
            if (MetadataSender.Enabled)
            {
                // start the timer again, as we need to make sure we're updating
                DataSender.Start();
                return;
            }

            // I believe the expected behaviour for https://github.com/speckleworks/SpeckleRhino/issues/286
            // is to send data regardless of wether the previous update was done. 
            if (IsSendingUpdate)// && !DebouncingDisabled)
            {
                return;
            }

            IsSendingUpdate = true;

            // Hack for thesis: always create history
            //var cloneResult = Client.StreamCloneAsync(StreamId).Result;
            //Client.Stream.Children.Add(cloneResult.Clone.StreamId);


            //Message = String.Format("Converting {0} \n objects", BucketObjects.Count);

            var convertedObjects = Converter.Serialise(BucketObjects).ToList();

            //Message = String.Format("Creating payloads");

            LocalContext.PruneExistingObjects(convertedObjects, Client.BaseUrl);

            List<SpeckleObject> persistedObjects = new List<SpeckleObject>();

            if (convertedObjects.Count(obj => obj.Type == "Placeholder") != convertedObjects.Count)
            {
                // create the update payloads
                int count = 0;
                var objectUpdatePayloads = new List<List<SpeckleObject>>();
                long totalBucketSize = 0;
                long currentBucketSize = 0;
                var currentBucketObjects = new List<SpeckleObject>();
                var allObjects = new List<SpeckleObject>();
                foreach (SpeckleObject convertedObject in convertedObjects)
                {

                    //if (count++ % 100 == 0)
                    //{
                    //    Message = "Converted " + count + " objects out of " + convertedObjects.Count() + ".";
                    //}

                    // size checking & bulk object creation payloads creation
                    long size = Converter.getBytes(convertedObject).Length;
                    currentBucketSize += size;
                    totalBucketSize += size;
                    currentBucketObjects.Add(convertedObject);

                    // Object is too big?
                    if (size > 2e6)
                    {

                        //AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "This stream contains a super big object. These will fail. Sorry for the bad error message - we're working on improving this.");

                        currentBucketObjects.Remove(convertedObject);
                    }

                    if (currentBucketSize > 5e5) // restrict max to ~500kb; should it be user config? anyway these functions should go into core. at one point. 
                    {
                        Debug.WriteLine("Reached payload limit. Making a new one, current  #: " + objectUpdatePayloads.Count);
                        objectUpdatePayloads.Add(currentBucketObjects);
                        currentBucketObjects = new List<SpeckleObject>();
                        currentBucketSize = 0;
                    }
                }

                // add in the last bucket
                if (currentBucketObjects.Count > 0)
                {
                    objectUpdatePayloads.Add(currentBucketObjects);
                }

                Debug.WriteLine("Finished, payload object update count is: " + objectUpdatePayloads.Count + " total bucket size is (kb) " + totalBucketSize / 1000);

                // create bulk object creation tasks
                int k = 0;
                List<ResponseObject> responses = new List<ResponseObject>();
                foreach (var payload in objectUpdatePayloads)
                {
                    //Message = String.Format("{0}/{1}", k++, objectUpdatePayloads.Count);

                    try
                    {
                        var objResponse = Client.ObjectCreateAsync(payload).Result;
                        responses.Add(objResponse);
                        persistedObjects.AddRange(objResponse.Resources);

                        int m = 0;
                        foreach (var oL in payload)
                        {
                            oL._id = objResponse.Resources[m++]._id;
                        }

                        // push sent objects in the cache non-blocking
                        Task.Run(() =>
                        {
                            foreach (var oL in payload)
                            {
                                if (oL.Type != "Placeholder")
                                {
                                    LocalContext.AddSentObject(oL, Client.BaseUrl);
                                }
                            }
                        });

                    }
                    catch (Exception err)
                    {
                        //AddRuntimeMessage(GH_RuntimeMessageLevel.Error, err.Message);
                        return;
                    }
                }
            }
            else
            {
                persistedObjects = convertedObjects;
            }

            // create placeholders for stream update payload
            List<SpeckleObject> placeholders = new List<SpeckleObject>();

            //foreach ( var myResponse in responses )
            foreach (var obj in persistedObjects)
            {
                placeholders.Add(new SpecklePlaceholder() { _id = obj._id });
            }

            SpeckleStream updateStream = new SpeckleStream()
            {
                Layers = BucketLayers,
                Name = BucketName,
                Objects = placeholders
            };

            //// set some base properties (will be overwritten)
            //var baseProps = new Dictionary<string, object>();
            //baseProps["units"] = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem.ToString();
            //baseProps["tolerance"] = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            //baseProps["angleTolerance"] = Rhino.RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
            //updateStream.BaseProperties = baseProps;

            var response = Client.StreamUpdateAsync(Client.StreamId, updateStream).Result;

            Client.BroadcastMessage("stream", Client.StreamId, new { eventType = "update-global" });

            //Log += response.Message;
            //AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Data sent at " + DateTime.Now);
            //Message = "Data sent\n@" + DateTime.Now.ToString("hh:mm:ss");

            IsSendingUpdate = false;
            //State = "Ok";
        }

        public List<object> GetData()
        {
            List<object> data = new List<dynamic>();
            //foreach (IGH_Param myParam in Params.Input)
            //{
            //    foreach (object o in myParam.VolatileData.AllData(false))
            //    {
            //        data.Add(o);
            //    }
            //}

            //data.Add(12345);
            //data.Add("abcde");

            data.Add(this.DSscene);

            //data = data.Select(obj =>
            //{
            //    try
            //    {
            //        return obj.GetType().GetProperty("Value").GetValue(obj);
            //    }
            //    catch
            //    {
            //        return null;
            //    }
            //}).ToList();

            return data;
        }

        public List<Layer> GetLayers()
        {
            List<Layer> layers = new List<Layer>();
            int startIndex = 0;
            int count = 0;
            //foreach (IGH_Param myParam in Params.Input)
            //{
            //    Layer myLayer = new Layer(
            //        myParam.NickName,
            //        myParam.InstanceGuid.ToString(),
            //        GetParamTopology(myParam),
            //        myParam.VolatileDataCount,
            //        startIndex,
            //        count);

            //    layers.Add(myLayer);
            //    startIndex += myParam.VolatileDataCount;
            //    count++;
            //}

            Layer myLayer = new Layer(
                "LayerName",
                System.Guid.NewGuid().ToString(),
                "topologyTEMP",
                1,
                startIndex,
                count);

            layers.Add(myLayer);

            return layers;
        }



    }


 }
