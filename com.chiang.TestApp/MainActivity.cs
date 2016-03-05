using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace com.chiang.TestApp {
    [Activity(Label = "com.chiang.TestApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity {

        private com.chiang.GestureLock.GestureLockThumbnail glt = null;
        private com.chiang.GestureLock.GestureLockViewGroup clvg = null;

        protected override void OnCreate(Bundle bundle) {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            glt = FindViewById<com.chiang.GestureLock.GestureLockThumbnail>(Resource.Id.id_gestureLockThumbnail);
            clvg = FindViewById<com.chiang.GestureLock.GestureLockViewGroup>(Resource.Id.id_gestureLockViewGroup);
            clvg.onPathPointPassed += clvg_onPathPointPassed;
            clvg.onGestureCompleted += clvg_onGestureCompleted;
            clvg.setDefAnswer(new int[] { 1, 2, 3, 6, 9 });
            clvg.showGesturePath(false);
            clvg.setUnMatchExceedBoundary(3);
        }

        void clvg_onGestureCompleted(GestureLock.GestureLockViewGroup.GestureCompletedArg obj) {
            var result = obj.result;
            int remain = obj.remainTryTimes;
            bool? matched = obj.matched,
                  outTryTime = obj.outTryTime;
            glt.SetReslut(result);
            if (matched.HasValue) {
                if (!matched.Value) {
                    if (outTryTime.Value) {
                        Toast.MakeText(this, "已达到最大重试次数", ToastLength.Short).Show();
                    }
                    else {
                        Toast.MakeText(this, string.Format("手势错误，剩余重试次数{0}", remain.ToString()), ToastLength.Short)
                            .Show();
                    }
                }
                else {
                    Toast.MakeText(this, "手势输入正确", ToastLength.Short)
                            .Show();
                }
            }
            else {
                Toast.MakeText(this, string.Join("", result.Select(arg => arg.ToString())), ToastLength.Short).Show();
            }
        }

        void clvg_onPathPointPassed(int obj) {
        }
    }
}

