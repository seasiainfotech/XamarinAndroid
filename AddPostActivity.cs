
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using ASEM.Droid.ViewModel.Academy.Adapter;
using ASEM.Social;
using Java.IO;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Xamarin.Essentials;
using static ASEM.Social.SocialFeedListeners;

namespace ASEM.Droid
{
    [Activity(Label = "AddPostActivity", Theme = "@style/MyTheme", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, WindowSoftInputMode = Android.Views.SoftInput.AdjustResize | Android.Views.SoftInput.AdjustPan)]
    public class AddPostActivity : BaseActivity, View.IOnClickListener, AndroidInterfaceUtils.IImagePicker,
    AndroidInterfaceUtils.IRequestPermissionListener, IAddSocialFeedListener, IRestSharpError
    {

        #region Global

        private LinearLayout ll_back, ll_post, ll_photovideo, ll_tag_friends, ll_camera;
        private int PicFromGallery = 102, REQUEST_TAKE_GALLERY_VIDEO = 123, CameraCaptureImageRequestCode = 100, CameraCaptureVideoRequestCode = 103, PicCrop = 101;
        private long imageSize;
        private string base64ImageCode;
        private static List<AddUpdateImageModel> imagesList = new List<AddUpdateImageModel>();
        private AddUpdateImageAdapter addUpdateImageAdapter;
        private RecyclerView horizontal_recycler_view;
        private Android.Net.Uri fileUri;
        private Bitmap newBitmap;
        private Refractored.Controls.CircleImageView cv_userprofile;
        private ImageView img_event1;
        private TextView tv_username;
        private bool CAMERA, SUCCESS;
        private List<byte[]> byteList = new List<byte[]>();
        private string TAG = "AddPostActivity";
        private EditText editTitle;

        #endregion


        #region init

        internal override int GetLayoutId()
        {
            return Resource.Layout.add_user_feed_layout;
        }

        internal override void InitListeners()
        {
            ll_post.SetOnClickListener(this);
            ll_back.SetOnClickListener(this);
            ll_photovideo.SetOnClickListener(this);
            ll_tag_friends.SetOnClickListener(this);
            ll_camera.SetOnClickListener(this);

        }



        internal override void InitViews()
        {
            editTitle = FindViewById<EditText>(Resource.Id.editTitle);
            cv_userprofile = FindViewById<Refractored.Controls.CircleImageView>(Resource.Id.cv_userprofile);
            horizontal_recycler_view = FindViewById<RecyclerView>(Resource.Id.horizontal_recycler_view);
            img_event1 = FindViewById<ImageView>(Resource.Id.img_event1);
            tv_username = FindViewById<TextView>(Resource.Id.tv_username);
            ll_back = (LinearLayout)FindViewById(Resource.Id.ll_back);
            ll_post = (LinearLayout)FindViewById(Resource.Id.ll_post);
            ll_photovideo = (LinearLayout)FindViewById(Resource.Id.ll_photovideo);
            ll_tag_friends = (LinearLayout)FindViewById(Resource.Id.ll_tag_friends);
            ll_camera = (LinearLayout)FindViewById(Resource.Id.ll_camera);

            LinearLayoutManager mLayoutManager = new LinearLayoutManager(this, LinearLayoutManager.Horizontal, false);
            horizontal_recycler_view.SetLayoutManager(mLayoutManager);
            imagesList.Clear();
            addUpdateImageAdapter = new AddUpdateImageAdapter(this, imagesList);
            horizontal_recycler_view.SetAdapter(addUpdateImageAdapter);

            tv_username.Text = Settings.UserFirstNameSettings + " " + Settings.UserMiddleNameSettings + " " + Settings.UserLastNameSettings;

            BasicUtils.GetInstance().SetImageFromUrl(this, this, Settings.UserImageUrlSettings, cv_userprofile, showMessageObject, false);

        }

        protected override void OnResume()
        {
            base.OnResume();

            LocationTracking();

        }
        #endregion

        #region Api

        private void AddPost()
        {

            if (NetworkConnectionClass.GetInstance().IsConnectedToInternet())
            {
                DisableViews();

                string postTitle = editTitle.Text.Trim();
                if (Settings.UserLatSettings.Length == 0)
                    Settings.UserLatSettings = "0.0";

                if (Settings.UserLongSettings.Length == 0)
                    Settings.UserLongSettings = "0.0";


                AddSocialFeedValidation feedValidation = new AddSocialFeedValidation(this, showLoadersObject, showMessageObject, this);
                feedValidation.Validation(int.Parse(Settings.UserIdSettings), postTitle, byteList, "",
                                          float.Parse(Settings.UserLatSettings), float.Parse(Settings.UserLongSettings), "");
            }

           

        }

        #endregion

        #region Clicklistener

        public void OnClick(View v)
        {
            switch (v.Id)
            {
                case Resource.Id.ll_back:

                    Finish();

                    break;

                case Resource.Id.ll_post:

                    AddPost();

                    break;

                case Resource.Id.ll_photovideo:
                    CAMERA = false;

                    AlertCameraGallery();

                    break;

                case Resource.Id.ll_tag_friends:

                    showMessageObject.ShowMessage("Coming Soon");

                    break;

                case Resource.Id.ll_camera:

                    CAMERA = true;

                    AlertCameraGallery();


                    break;
                default:

                    break;
            }
        }
        #endregion

        #region dialog

        public void OpenAlertDialog(string message)
        {

            Dialog dialog = new Dialog(this, Resource.Style.Theme_CustomDialog);
            dialog.SetCancelable(false);
            dialog.SetContentView(Resource.Layout.custom_alert_dialog);
            dialog.Window.SetBackgroundDrawableResource(Android.Resource.Color.Transparent);

            TextView tv_message = dialog.FindViewById<TextView>(Resource.Id.tv_message);
            TextView tv_no = dialog.FindViewById<TextView>(Resource.Id.tv_no);
            TextView tv_yes = dialog.FindViewById<TextView>(Resource.Id.tv_yes);
            tv_no.Visibility = ViewStates.Gone;
            tv_yes.Text = "Ok";
            tv_message.Text = message;

            tv_yes.Click += delegate
            {
                if (SUCCESS)
                {
                    Finish();
                }
                dialog.Dismiss();
            };
            tv_no.Click += delegate
            {
                dialog.Dismiss();
            };
            dialog.Show();
        }

        public void AlertCameraGallery()
        {
            AlertDialog alertDialog = new AlertDialog.Builder(context, AlertDialog.ThemeHoloLight).Create();
            var dialougeView = activity.LayoutInflater.Inflate(Resource.Layout.custom_image_picker_layout, null);

            alertDialog.SetTitle("Complete action using");
            alertDialog.SetView(dialougeView);
            TextView cameraTextView = dialougeView.FindViewById<TextView>(Resource.Id.camera_txt);
            TextView galleryTextView = dialougeView.FindViewById<TextView>(Resource.Id.gallery_txt);
            Button cancelBtn = dialougeView.FindViewById<Button>(Resource.Id.cancel);

            galleryTextView.Text = "Photo";
            cameraTextView.Text = "Video";

            if (!((Activity)context).IsFinishing)
            {
                //show dialog
                alertDialog.Show();
            }

            // cancel the alert 
            cancelBtn.Click += delegate
            {
                alertDialog.Dismiss();
                alertDialog.Cancel();
            };

            // open the gallery for image
            galleryTextView.Click += delegate
            {// select the Photo
                if (CAMERA)
                {
                    Intent intent = new Intent(MediaStore.ActionImageCapture);
                    fileUri = CameraGalleryClass.GetInstance(context).GetTempFile();
                    intent.PutExtra(MediaStore.ExtraOutput, fileUri);
                    // start the image capture Intent
                    activity.StartActivityForResult(intent, CameraCaptureImageRequestCode);

                }
                else
                {
                    Intent intent = new Intent(Intent.ActionPick, MediaStore.Images.Media.ExternalContentUri);
                    activity.StartActivityForResult(intent, PicFromGallery);

                }

                alertDialog.Dismiss();
            };

            // open the gallery for video 
            cameraTextView.Click += delegate
            {
                // select the VIDEO
                showMessageObject.ShowMessage(Resources.GetString(Resource.String.coming_soon));
                //if (CAMERA)
                //{

                //    Random randomNumber = new Random();
                //    Java.IO.File videoFile = new Java.IO.File(Android.OS.Environment.ExternalStorageDirectory + "/com.Xamarin.Asem/Videos/");
                //    if (!videoFile.Exists())
                //    {
                //        videoFile.Mkdirs();
                //    }


                //    Intent intent = new Intent(MediaStore.ActionVideoCapture);
                //    Android.Net.Uri videoFileUri = Android.Net.Uri.FromFile(new Java.IO.File(videoFile, "vid_" + randomNumber.Next() + ".mp4"));
                //    intent.PutExtra(MediaStore.ExtraOutput, fileUri);
                //    activity.StartActivityForResult(intent, CameraCaptureVideoRequestCode);
                //}
                //else
                //{
                //    Intent intent = new Intent();
                //    intent.SetType("video/*");
                //    intent.SetAction(Intent.ActionGetContent);
                //    StartActivityForResult(Intent.CreateChooser(intent, "Select Video"), REQUEST_TAKE_GALLERY_VIDEO);
                //}
                alertDialog.Dismiss();

            };
        }

        #endregion

        #region Image/Video

        public void DeleteImage(int position)
        {
            PrintLogDetails.GetInstance().PrintLogDeatails(TAG, "Delete Image ", "" + imagesList.Count);
            addUpdateImageAdapter.NotifyDataSetChanged();
            byteList.RemoveAt(position);
        }


        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode == Result.Ok)
            {
                if (requestCode == REQUEST_TAKE_GALLERY_VIDEO)
                {
                    horizontal_recycler_view.Visibility = ViewStates.Visible;
                    var uri = data.Data;
                    string path = BasicUtils.GetActualPathFromFile(uri, this);
                    System.Diagnostics.Debug.WriteLine("Image path == " + path);
                    Bitmap thumb = ThumbnailUtils.CreateVideoThumbnail(path, ThumbnailKind.MiniKind);

                    AddUpdateImageModel addUpdateImage = new AddUpdateImageModel();
                    base64ImageCode = BasicUtils.GetInstance().ConvertBitmapToBase64(thumb);


                    addUpdateImage.bitmap = thumb;
                    addUpdateImage.ImageUrl = base64ImageCode;
                    addUpdateImage.CountImage = imagesList.Count + 1;
                    imagesList.Add(addUpdateImage);
                    addUpdateImageAdapter.NotifyDataSetChanged();
                    horizontal_recycler_view.SmoothScrollToPosition(imagesList.Count());
                 
                }
                if (requestCode == CameraCaptureImageRequestCode)
                {
                    PreviewCapturedImage();
                }
                else if (requestCode == CameraCaptureVideoRequestCode)
                {
                    horizontal_recycler_view.Visibility = ViewStates.Visible;
                    var uri = data.Data;
                    string path = BasicUtils.GetActualPathFromFile(uri, this);
                    System.Diagnostics.Debug.WriteLine("Image path == " + path);
                    Bitmap thumb = ThumbnailUtils.CreateVideoThumbnail(path, ThumbnailKind.MiniKind);

                    AddUpdateImageModel addUpdateImage = new AddUpdateImageModel();
                    base64ImageCode = BasicUtils.GetInstance().ConvertBitmapToBase64(thumb);


                    addUpdateImage.bitmap = thumb;
                    addUpdateImage.ImageUrl = base64ImageCode;
                    addUpdateImage.CountImage = imagesList.Count + 1;
                    imagesList.Add(addUpdateImage);
                    addUpdateImageAdapter.NotifyDataSetChanged();
                    horizontal_recycler_view.SmoothScrollToPosition(imagesList.Count());

                }
                else if (requestCode == PicFromGallery)
                {

                    fileUri = data.Data;

                    Bitmap bitmap = MediaStore.Images.Media.GetBitmap(ContentResolver, fileUri);


                    GetImageFromGallery();
                }
                else if (requestCode == PicCrop && data != null)
                {
                    //get the returned data 
                    Android.Net.Uri extras = data.Data;
                    //get the cropped bitmap

                    if (extras != null)
                    {
                        var inp = context.ContentResolver.OpenInputStream(extras);
                        BitmapFactory.Options options = new BitmapFactory.Options();
                        Bitmap cropped_bitmap = BitmapFactory.DecodeStream(inp, null, options);
                        newBitmap = cropped_bitmap;
                        getImageBitmapAsync(newBitmap);

                    }
                    else{
                        var inp = context.ContentResolver.OpenInputStream(fileUri);
                        BitmapFactory.Options options = new BitmapFactory.Options();


                        //Bitmap cropped_bitmap =
                        MemoryStream stream = new MemoryStream();
                        BitmapFactory.DecodeStream(inp, null, options).Compress(Bitmap.CompressFormat.Jpeg, 50, stream);
                      
                        byte[] bitmapData = stream.ToArray();
                        Bitmap bitmap = BitmapFactory.DecodeByteArray(bitmapData, 0, bitmapData.Length);


                        newBitmap = bitmap;
                        getImageBitmapAsync(newBitmap);
                    }
                }

            }
        }


        private void GetImageFromGallery()
        {
            try
            {
                DecodeBitmap(PicFromGallery);
                if (!CameraGalleryClass.GetInstance(context).GetDeviceModelNumber().Contains("Nexus"))
                {
                    PerformCrop();
                }
                else
                {
                    getImageBitmapAsync(newBitmap);
                }

            }
            catch (Java.Lang.Exception e)
            {
                PrintLogDetails.GetInstance().PrintErrorDetails("", "GetImageFromGallery", e.ToString());
            }
        }


        private void PreviewCapturedImage()
        {
            try
            {
                DecodeBitmap(CameraCaptureImageRequestCode);
                //rotateImageBy90Degree ();
                if (!CameraGalleryClass.GetInstance(context).GetDeviceModelNumber().Contains("Nexus"))
                    PerformCrop();
                else
                    getImageBitmapAsync(newBitmap);

            }
            catch (Java.Lang.Exception e)
            {
                showMessageObject.ShowMessage(e.ToString());
            }
        }

        private void DecodeBitmap(int resultcode)
        {
            Bitmap bitmap = null;
            try
            {
                BitmapFactory.Options o = new BitmapFactory.Options();
                o.InJustDecodeBounds = true;
                if (resultcode == CameraCaptureImageRequestCode)
                {
                    if (fileUri != null)
                        bitmap = BitmapFactory.DecodeFile(fileUri.Path, o);
                }
                else
                {
                    bitmap = BitmapFactory.DecodeStream(context.ContentResolver.OpenInputStream(fileUri));
                }
                //final int REQUIRED_SIZE = 180;
                int width_tmp = o.OutWidth, height_tmp = o.OutHeight;
                int scale = 1;
                while (true)
                {
                    if (width_tmp / 2 < 180 || height_tmp / 2 < 180)
                    {
                        break;
                    }
                    width_tmp /= 2;
                    height_tmp /= 2;
                    scale *= 2;
                }
                bitmap = null;
                BitmapFactory.Options o2 = new BitmapFactory.Options();
                o2.InSampleSize = scale;
                if (resultcode == CameraCaptureImageRequestCode)
                {
                    bitmap = BitmapFactory.DecodeFile(fileUri.Path, o2);
                }
                else
                {
                    bitmap = BitmapFactory.DecodeStream(context.ContentResolver.OpenInputStream(fileUri));
                }
                newBitmap = bitmap;
            }
            catch (Exception e)
            {
                showMessageObject.ShowMessage(e.ToString());
            }
        }


        public void getImageBitmapAsync(Bitmap bitmap)
        {
            horizontal_recycler_view.Visibility = ViewStates.Visible;



            var stream = new MemoryStream();
            bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
            byte[] imageInByte = stream.ToArray();
            byteList.Add(imageInByte);

            long lengthbmp = imageInByte.Length;

            imageSize = imageSize + lengthbmp;

            AddUpdateImageModel addUpdateImage = new AddUpdateImageModel();
            base64ImageCode = BasicUtils.GetInstance().ConvertBitmapToBase64(bitmap);
            img_event1.SetImageBitmap(bitmap);

            addUpdateImage.bitmap = bitmap;
            addUpdateImage.ImageUrl = base64ImageCode;
            addUpdateImage.CountImage = imagesList.Count + 1;
            imagesList.Add(addUpdateImage);
            addUpdateImageAdapter.NotifyDataSetChanged();
            horizontal_recycler_view.SmoothScrollToPosition(imagesList.Count());
        }

        private void PerformCrop()
        {
            try
            {
                //call the standard crop action intent (the user device may not support it)
                Intent cropIntent = new Intent("com.android.camera.action.CROP");
                //indicate image type and Uri
                cropIntent.SetDataAndType(fileUri, "image/*");
                //set crop properties
                cropIntent.PutExtra("crop", "true");
                //indicate aspect of desired crop
                cropIntent.PutExtra("aspectX", 1);
                cropIntent.PutExtra("aspectY", 1);
                //indicate output X and Y
                cropIntent.PutExtra("outputX", 256);
                cropIntent.PutExtra("outputY", 256);
                //retrieve data on return
                cropIntent.PutExtra("return-data", true);

                Android.Net.Uri tempURI = CameraGalleryClass.GetInstance(context).GetTempFile();
                cropIntent.PutExtra(MediaStore.ExtraOutput, tempURI);
                //start the activity - we handle returning in onActivityResult
                activity.StartActivityForResult(cropIntent, PicCrop);
            }
            catch (ActivityNotFoundException anfe)
            {
                //display an error message
                showMessageObject.ShowMessage("Your_crop_action" + anfe);

            }
            catch (Java.Lang.Exception e)
            {
                showMessageObject.ShowMessage(e.ToString());
            }
        }

        public void requestPermission(string[] permissionsList)
        {
            RequestPermissions(permissionsList, 0);
        }

        public int GetOriginalLengthInBytes(string base64string)
        {
            if (string.IsNullOrEmpty(base64string)) { return 0; }

            var characterCount = base64string.Length;
            var paddingCount = base64string.Substring(characterCount - 2, 2)
                                           .Count(c => c == '=');
            return (3 * (characterCount / 4)) - paddingCount;
        }


        #endregion

        #region Location
        private void LocationTracking()
        {
            FetchLocationAsync();
        }

        async void FetchLocationAsync()
        {
            var hasPermission = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
            if (hasPermission != PermissionStatus.Granted)
            {
                var requestedResponse = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Location);
                if (requestedResponse[Permission.Location] != PermissionStatus.Granted)
                {

                    PrintLogDetails.GetInstance().PrintLogDeatails("Select Location", "Unable to get location: ", "");
                    return;
                }
            }
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                if (location != null)
                {
                    Settings.UserLatSettings = location.Latitude.ToString();
                    Settings.UserLongSettings = location.Longitude.ToString();
                    System.Console.WriteLine("latitude: " + location.Latitude);
                    System.Console.WriteLine("Longitude: " + location.Longitude);

                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                System.Console.WriteLine("Longitude: " + fnsEx);
                // Handle not supported on device exception
            }
            catch (PermissionException pEx)
            {
                // Handle permission exception
                System.Console.WriteLine("Longitude: " + pEx);
            }


        }
        #endregion


        #region result

        public void AddSocialFeedResult(AddSocialFeed addSocialFeed)
        {
            RunOnUiThread(() =>
            {
                if(addSocialFeed.statusCode == 200)
                {
                   
                    SUCCESS = true;
                    OpenAlertDialog("Post Added Successfully!");
                }
                else{
                    EnableViews();
                    SUCCESS = false;
                    OpenAlertDialog(addSocialFeed.Message);
                }
            });
        }

        public void ShowRestSharpServiceError(string error)
        {
            RunOnUiThread(() =>
            {
                EnableViews();
                SUCCESS = false;
                OpenAlertDialog(error);
            });
        }


        #endregion


        #region Enable Disable Views

        public void DisableViews()
        {
            ll_post.Enabled = false;
            ll_back.Enabled = false;
            ll_photovideo.Enabled = false;
            ll_tag_friends.Enabled = false;
            ll_camera.Enabled = false;
            editTitle.Enabled = false;

        }

        public void EnableViews()
        {
            ll_post.Enabled = true;
            ll_back.Enabled = true;
            ll_photovideo.Enabled = true;
            ll_tag_friends.Enabled = true;
            ll_camera.Enabled = true;
            editTitle.Enabled = true;

        }

        #endregion

    }
}
