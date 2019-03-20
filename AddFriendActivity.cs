using System.Collections.Generic;
using Android.App;
using Android.Content.PM;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Widget;
using Java.Lang;

namespace ASEM.Droid
{
    [Activity(Label = "AddFriendActivity", Theme = "@style/MyTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class AddFriendActivity : BaseActivity, ITextWatcher, SocialCircleListeners.ISearchUsersListener, IRestSharpError
    {
        #region Global Variables
        EditText textViewSearch;
        RecyclerView recyclerView;
        List<User> usersList;
        int page;
        string enteredText;
        FriendsListAdapter friendsListAdapter;
        #endregion
        #region Listeners
        internal override void InitListeners()
        {
            textViewSearch.AddTextChangedListener(this);

        }

        public void AfterTextChanged(IEditable s)
        {
        }

        public void BeforeTextChanged(ICharSequence chars, int start, int count, int after)
        {
        }


        public void OnTextChanged(ICharSequence chars, int start, int before, int count)
        {
            enteredText = chars.ToString();
            SearchUsersApi(enteredText);
        }
        #endregion
        #region Layout Views
        internal override int GetLayoutId()
        {
            return Resource.Layout.social_friends_list_layout;
        }

        internal override void InitViews()
        {
            page = 1;
            textViewSearch = FindViewById<EditText>(Resource.Id.edt_search);
            recyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView);
            GridLayoutManager mLayoutManager = new GridLayoutManager(this, 1, LinearLayoutManager.Vertical, false);
            ImageView imgBack = FindViewById<ImageView>(Resource.Id.back);
            imgBack.Click += delegate
            {
                Finish();
            };
            recyclerView.SetLayoutManager(mLayoutManager);
            enteredText = string.Empty;
            SearchUsersApi(enteredText);
        }

        private void PopulateRecyclerViewAdapterData()
        {
            usersList = new List<User>();
            friendsListAdapter = new FriendsListAdapter(this, usersList, enteredText);
            recyclerView.SetAdapter(friendsListAdapter);

        }
        #endregion
        #region Api Integration
        public void SearchUsersApi(string enteredText)
        {
            SearchUsersApi searchUsers = new SearchUsersApi(this, this, showLoadersObject, showMessageObject);
            searchUsers.SearchUsers(enteredText, page);

        }
        public void SearchUserResult(SearchUsersModel searchUsersModel)
        {
            if (searchUsersModel.User != null && searchUsersModel.User.Count != 0)
            {
                usersList = searchUsersModel.User;
                RunOnUiThread(delegate
                {
                    friendsListAdapter = new FriendsListAdapter(this, usersList, enteredText);
                    recyclerView.SetAdapter(friendsListAdapter);
                    friendsListAdapter.NotifyDataSetChanged();

                });
            }

        }

        public void ShowRestSharpServiceError(string error)
        {
            RunOnUiThread(delegate
            {
                showMessageObject.ShowMessage(error);

            });
        }
        #endregion
    }
}

