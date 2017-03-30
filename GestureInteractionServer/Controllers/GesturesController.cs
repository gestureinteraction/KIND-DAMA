using System;
using System.Web.Http;
using System.Web.Http.Cors;
using Managers;
using Models;
using Utilities.Logger;


namespace Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class GesturesController : ApiController
    {
        [HttpPost]
        public bool Save([FromBody] JointGestureCollectionElement gesture)
        {
            Logging.Instance.Information(this.GetType(), "Received cmd: /gestures/save {0}", gesture);

            try
            {
                bool insert= DBManager.Instance.insertGesture(gesture.tag, gesture.device_id, gesture.stream, gesture.frames);
                Logging.Instance.Information(this.GetType(), "Gesture saved:" + insert);
                return insert;
            }
            catch (Exception e)
            {
                Logging.Instance.Error(this.GetType(), "Errore salvataggio gesto: {0}", e);
                return false;
            }
        }

        [HttpGet]
        public IHttpActionResult Show(string devId, string streamName, string gestureTag, int gesturePos)
        {
            Logging.Instance.Information(this.GetType(), "Received cmd: /gestures/show/?devId={0}&streamName={1}&gestureTag={2}&gesturePos={3}", devId, streamName, gestureTag, gesturePos);

            if ((gestureTag != null) && (gestureTag.Length > 0))
            {
                return Ok(DBManager.Instance.retrieve3DGesture(devId, streamName, gestureTag, gesturePos));
            }
            else
            {
//                return Ok(DBManager.Instance.retrieve3DGesture(devId, streamName, gestureTag, gesturePos));
                return Ok("NO");
            }
        }

        [HttpGet]
        public IHttpActionResult Counter(string devId, string streamName, string gestureTag)
        {
            Logging.Instance.Information(this.GetType(), "Received cmd: /gestures/?devId={0}&streamName={1}&gestureTag={2}", devId, streamName, gestureTag);

            return Ok(DBManager.Instance.retrieve3DGesturesCounter(devId, streamName, gestureTag));
        }

        [HttpPost]
        public IHttpActionResult Update([FromBody] JointGestureCollectionElement gesture)
        {
            Logging.Instance.Information(this.GetType(), "Received cmd: /gestures/update/?gesture={1}", gesture);
            return Ok(DBManager.Instance.updateGesture(gesture));
           

        }

        [HttpGet]
        public IHttpActionResult Delete(string devId, string streamName, string gesturePos)
        {
            Logging.Instance.Information(this.GetType(), "Received cmd: /gestures/delete/?devId={0}&streamName={1}&gesturePos={2}", devId, streamName,gesturePos);

            return Ok(DBManager.Instance.deleteGesture(devId, streamName, gesturePos));
            //            return Ok(DBManager.Instance.retrieve3DGesturesCounter(devId, streamName));
        }
    }
}