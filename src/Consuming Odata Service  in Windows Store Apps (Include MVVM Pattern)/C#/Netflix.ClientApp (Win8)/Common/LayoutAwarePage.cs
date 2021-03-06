﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LayoutAwarePage.cs" company="saramgsilva">
//   Copyright (c) 2012 saramgsilva. All rights reserved.
// </copyright>
// <summary>
//   Typical implementation of Page that provides several important conveniences:
//   <list type="bullet">
//   <item>
//   <description>Application view state to visual state mapping</description>
//   </item>
//   <item>
//   <description>GoBack, GoForward, and GoHome event handlers</description>
//   </item>
//   <item>
//   <description>Mouse and keyboard shortcuts for navigation</description>
//   </item>
//   <item>
//   <description>State management for navigation and process lifetime management</description>
//   </item>
//   <item>
//   <description>A default view model</description>
//   </item>
//   </list>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Netflix.ClientApp.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using Windows.ApplicationModel;
    using Windows.Foundation.Collections;
    using Windows.Foundation.Metadata;
    using Windows.System;
    using Windows.UI.Core;
    using Windows.UI.Input;
    using Windows.UI.ViewManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    /// <summary>
    /// Typical implementation of Page that provides several important conveniences:
    /// <list type="bullet">
    /// <item>
    /// <description>Application view state to visual state mapping</description>
    /// </item>
    /// <item>
    /// <description>GoBack, GoForward, and GoHome event handlers</description>
    /// </item>
    /// <item>
    /// <description>Mouse and keyboard shortcuts for navigation</description>
    /// </item>
    /// <item>
    /// <description>State management for navigation and process lifetime management</description>
    /// </item>
    /// <item>
    /// <description>A default view model</description>
    /// </item>
    /// </list>
    /// </summary>
    [WebHostHidden]
    public class LayoutAwarePage : Page
    {
        #region Static Fields

        /// <summary>
        /// Identifies the <see cref="DefaultViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DefaultViewModelProperty =
            DependencyProperty.Register(
                "DefaultViewModel", typeof(IObservableMap<string, object>), typeof(LayoutAwarePage), null);

        #endregion

        #region Fields

        /// <summary>
        /// The _layout aware controls.
        /// </summary>
        private List<Control> _layoutAwareControls;

        /// <summary>
        /// The _page key.
        /// </summary>
        private string _pageKey;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutAwarePage"/> class.
        /// </summary>
        public LayoutAwarePage()
        {
            if (DesignMode.DesignModeEnabled)
            {
                return;
            }

            // Create an empty default view model
            this.DefaultViewModel = new ObservableDictionary<string, object>();

            // When this page is part of the visual tree make two changes:
            // 1) Map application view state to visual state for the page
            // 2) Handle keyboard and mouse navigation requests
            this.Loaded += (sender, e) =>
                {
                    this.StartLayoutUpdates(sender, e);

                    // Keyboard and mouse navigation only apply when occupying the entire window
                    if (this.ActualHeight == Window.Current.Bounds.Height
                        && this.ActualWidth == Window.Current.Bounds.Width)
                    {
                        // Listen to the window directly so focus isn't required
                        Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated +=
                            this.CoreDispatcher_AcceleratorKeyActivated;
                        Window.Current.CoreWindow.PointerPressed += this.CoreWindow_PointerPressed;
                    }
                };

            // Undo the same changes when the page is no longer visible
            this.Unloaded += (sender, e) =>
                {
                    this.StopLayoutUpdates(sender, e);
                    Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated -=
                        this.CoreDispatcher_AcceleratorKeyActivated;
                    Window.Current.CoreWindow.PointerPressed -= this.CoreWindow_PointerPressed;
                };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets an implementation of <see cref="IObservableMap&lt;String, Object&gt;" /> designed to be
        /// used as a trivial view model.
        /// </summary>
        /// <value>
        /// The default view model.
        /// </value>
        protected IObservableMap<string, object> DefaultViewModel
        {
            get
            {
                return this.GetValue(DefaultViewModelProperty) as IObservableMap<string, object>;
            }

            set
            {
                this.SetValue(DefaultViewModelProperty, value);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Updates all controls that are listening for visual state changes with the correct
        /// visual state.
        /// </summary>
        /// <remarks>
        /// Typically used in conjunction with overriding <see cref="DetermineVisualState"/> to
        /// signal that a different value may be returned even though the view state has not
        /// changed.
        /// </remarks>
        public void InvalidateVisualState()
        {
            if (this._layoutAwareControls != null)
            {
                string visualState = this.DetermineVisualState(ApplicationView.Value);
                foreach (Control layoutAwareControl in this._layoutAwareControls)
                {
                    VisualStateManager.GoToState(layoutAwareControl, visualState, false);
                }
            }
        }

        /// <summary>
        /// Invoked as an event handler, typically on the <see cref="FrameworkElement.Loaded"/>
        /// event of a <see cref="Control"/> within the page, to indicate that the sender should
        /// start receiving visual state management changes that correspond to application view
        /// state changes.
        /// </summary>
        /// <param name="sender">
        /// Instance of <see cref="Control"/> that supports visual state
        /// management corresponding to view states.
        /// </param>
        /// <param name="e">
        /// Event data that describes how the request was made.
        /// </param>
        /// <remarks>
        /// The current view state will immediately be used to set the corresponding
        /// visual state when layout updates are requested.  A corresponding
        /// <see cref="FrameworkElement.Unloaded"/> event handler connected to
        /// <see cref="StopLayoutUpdates"/> is strongly encouraged.  Instances of
        /// <see cref="LayoutAwarePage"/> automatically invoke these handlers in their Loaded and
        /// Unloaded events.
        /// </remarks>
        /// <seealso cref="DetermineVisualState"/>
        /// <seealso cref="InvalidateVisualState"/>
        public void StartLayoutUpdates(object sender, RoutedEventArgs e)
        {
            var control = sender as Control;
            if (control == null)
            {
                return;
            }

            if (this._layoutAwareControls == null)
            {
                // Start listening to view state changes when there are controls interested in updates
                Window.Current.SizeChanged += this.WindowSizeChanged;
                this._layoutAwareControls = new List<Control>();
            }

            this._layoutAwareControls.Add(control);

            // Set the initial visual state of the control
            VisualStateManager.GoToState(control, this.DetermineVisualState(ApplicationView.Value), false);
        }

        /// <summary>
        /// Invoked as an event handler, typically on the <see cref="FrameworkElement.Unloaded"/>
        /// event of a <see cref="Control"/>, to indicate that the sender should start receiving
        /// visual state management changes that correspond to application view state changes.
        /// </summary>
        /// <param name="sender">
        /// Instance of <see cref="Control"/> that supports visual state
        /// management corresponding to view states.
        /// </param>
        /// <param name="e">
        /// Event data that describes how the request was made.
        /// </param>
        /// <remarks>
        /// The current view state will immediately be used to set the corresponding
        /// visual state when layout updates are requested.
        /// </remarks>
        /// <seealso cref="StartLayoutUpdates"/>
        public void StopLayoutUpdates(object sender, RoutedEventArgs e)
        {
            var control = sender as Control;
            if (control == null || this._layoutAwareControls == null)
            {
                return;
            }

            this._layoutAwareControls.Remove(control);
            if (this._layoutAwareControls.Count == 0)
            {
                // Stop listening to view state changes when no controls are interested in updates
                this._layoutAwareControls = null;
                Window.Current.SizeChanged -= this.WindowSizeChanged;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Translates <see cref="ApplicationViewState"/> values into strings for visual state
        /// management within the page.  The default implementation uses the names of enum values.
        /// Subclasses may override this method to control the mapping scheme used.
        /// </summary>
        /// <param name="viewState">
        /// View state for which a visual state is desired.
        /// </param>
        /// <returns>
        /// Visual state name used to drive the
        /// <see cref="VisualStateManager"/>
        /// </returns>
        /// <seealso cref="InvalidateVisualState"/>
        protected virtual string DetermineVisualState(ApplicationViewState viewState)
        {
            return viewState.ToString();
        }

        /// <summary>
        /// Invoked as an event handler to navigate backward in the navigation stack
        /// associated with this page's <see cref="Frame"/>.
        /// </summary>
        /// <param name="sender">
        /// Instance that triggered the event.
        /// </param>
        /// <param name="e">
        /// Event data describing the conditions that led to the
        /// event.
        /// </param>
        protected virtual void GoBack(object sender, RoutedEventArgs e)
        {
            // Use the navigation frame to return to the previous page
            if (this.Frame != null && this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        /// <summary>
        /// Invoked as an event handler to navigate forward in the navigation stack
        /// associated with this page's <see cref="Frame"/>.
        /// </summary>
        /// <param name="sender">
        /// Instance that triggered the event.
        /// </param>
        /// <param name="e">
        /// Event data describing the conditions that led to the
        /// event.
        /// </param>
        protected virtual void GoForward(object sender, RoutedEventArgs e)
        {
            // Use the navigation frame to move to the next page
            if (this.Frame != null && this.Frame.CanGoForward)
            {
                this.Frame.GoForward();
            }
        }

        /// <summary>
        /// Invoked as an event handler to navigate backward in the page's associated
        /// <see cref="Frame"/> until it reaches the top of the navigation stack.
        /// </summary>
        /// <param name="sender">
        /// Instance that triggered the event.
        /// </param>
        /// <param name="e">
        /// Event data describing the conditions that led to the event.
        /// </param>
        protected virtual void GoHome(object sender, RoutedEventArgs e)
        {
            // Use the navigation frame to return to the topmost page
            if (this.Frame != null)
            {
                while (this.Frame.CanGoBack)
                {
                    this.Frame.GoBack();
                }
            }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">
        /// The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">
        /// A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.
        /// </param>
        protected virtual void LoadState(object navigationParameter, Dictionary<string, object> pageState)
        {
        }

        /// <summary>
        /// Invoked when this page will no longer be displayed in a Frame.
        /// </summary>
        /// <param name="e">
        /// Event data that describes how this page was reached.  The Parameter
        /// property provides the group to be displayed.
        /// </param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Dictionary<string, object> frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            var pageState = new Dictionary<string, object>();
            this.SaveState(pageState);
            var pageKey = this._pageKey;
            if (pageKey != null)
            {
                frameState[pageKey] = pageState;
            }
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">
        /// Event data that describes how this page was reached.  The Parameter
        /// property provides the group to be displayed.
        /// </param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Returning to a cached page through navigation shouldn't trigger state loading
            if (this._pageKey != null)
            {
                return;
            }

            Dictionary<string, object> frameState = SuspensionManager.SessionStateForFrame(this.Frame);
            this._pageKey = "Page-" + this.Frame.BackStackDepth;

            if (e.NavigationMode == NavigationMode.New)
            {
                // Clear existing state for forward navigation when adding a new page to the
                // navigation stack
                string nextPageKey = this._pageKey;
                int nextPageIndex = this.Frame.BackStackDepth;
                while (frameState.Remove(nextPageKey))
                {
                    nextPageIndex++;
                    nextPageKey = "Page-" + nextPageIndex;
                }

                // Pass the navigation parameter to the new page
                this.LoadState(e.Parameter, null);
            }
            else
            {
                // Pass the navigation parameter and preserved page state to the page, using
                // the same strategy for loading suspended state and recreating pages discarded
                // from cache
                this.LoadState(e.Parameter, (Dictionary<string, object>)frameState[this._pageKey]);
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">
        /// An empty dictionary to be populated with serializable state.
        /// </param>
        protected virtual void SaveState(Dictionary<string, object> pageState)
        {
        }

        /// <summary>
        /// Invoked on every keystroke, including system keys such as Alt key combinations, when
        /// this page is active and occupies the entire window.  Used to detect keyboard navigation
        /// between pages even when the page itself doesn't have focus.
        /// </summary>
        /// <param name="sender">
        /// Instance that triggered the event.
        /// </param>
        /// <param name="args">
        /// Event data describing the conditions that led to the event.
        /// </param>
        private void CoreDispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            VirtualKey virtualKey = args.VirtualKey;

            // Only investigate further when Left, Right, or the dedicated Previous or Next keys
            // are pressed
            if ((args.EventType == CoreAcceleratorKeyEventType.SystemKeyDown
                 || args.EventType == CoreAcceleratorKeyEventType.KeyDown)
                &&
                (virtualKey == VirtualKey.Left || virtualKey == VirtualKey.Right || (int)virtualKey == 166
                 || (int)virtualKey == 167))
            {
                CoreWindow coreWindow = Window.Current.CoreWindow;
                var downState = CoreVirtualKeyStates.Down;
                bool menuKey = (coreWindow.GetKeyState(VirtualKey.Menu) & downState) == downState;
                bool controlKey = (coreWindow.GetKeyState(VirtualKey.Control) & downState) == downState;
                bool shiftKey = (coreWindow.GetKeyState(VirtualKey.Shift) & downState) == downState;
                bool noModifiers = !menuKey && !controlKey && !shiftKey;
                bool onlyAlt = menuKey && !controlKey && !shiftKey;

                if (((int)virtualKey == 166 && noModifiers) || (virtualKey == VirtualKey.Left && onlyAlt))
                {
                    // When the previous key or Alt+Left are pressed navigate back
                    args.Handled = true;
                    this.GoBack(this, new RoutedEventArgs());
                }
                else if (((int)virtualKey == 167 && noModifiers) || (virtualKey == VirtualKey.Right && onlyAlt))
                {
                    // When the next key or Alt+Right are pressed navigate forward
                    args.Handled = true;
                    this.GoForward(this, new RoutedEventArgs());
                }
            }
        }

        /// <summary>
        /// Invoked on every mouse click, touch screen tap, or equivalent interaction when this
        /// page is active and occupies the entire window.  Used to detect browser-style next and
        /// previous mouse button clicks to navigate between pages.
        /// </summary>
        /// <param name="sender">
        /// Instance that triggered the event.
        /// </param>
        /// <param name="args">
        /// Event data describing the conditions that led to the event.
        /// </param>
        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            PointerPointProperties properties = args.CurrentPoint.Properties;

            // Ignore button chords with the left, right, and middle buttons
            if (properties.IsLeftButtonPressed || properties.IsRightButtonPressed || properties.IsMiddleButtonPressed)
            {
                return;
            }

            // If back or foward are pressed (but not both) navigate appropriately
            bool backPressed = properties.IsXButton1Pressed;
            bool forwardPressed = properties.IsXButton2Pressed;
            if (backPressed ^ forwardPressed)
            {
                args.Handled = true;
                if (backPressed)
                {
                    this.GoBack(this, new RoutedEventArgs());
                }

                if (forwardPressed)
                {
                    this.GoForward(this, new RoutedEventArgs());
                }
            }
        }

        /// <summary>
        /// The window size changed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void WindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            this.InvalidateVisualState();
        }

        #endregion

        /// <summary>
        /// Implementation of IObservableMap that supports reentrancy for use as a default view
        /// model.
        /// </summary>
        /// <typeparam name="K">
        /// The k type
        /// </typeparam>
        /// <typeparam name="V">
        /// The V type
        /// </typeparam>
        private class ObservableDictionary<K, V> : IObservableMap<K, V>
        {
            #region Fields

            /// <summary>
            /// The _dictionary.
            /// </summary>
            private readonly Dictionary<K, V> _dictionary = new Dictionary<K, V>();

            #endregion

            #region Public Events

            /// <summary>
            /// The map changed.
            /// </summary>
            public event MapChangedEventHandler<K, V> MapChanged;

            #endregion

            #region Public Properties

            /// <summary>
            /// Gets the count.
            /// </summary>
            public int Count
            {
                get
                {
                    return this._dictionary.Count;
                }
            }

            /// <summary>
            /// Gets a value indicating whether is read only.
            /// </summary>
            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            /// <summary>
            /// Gets the keys.
            /// </summary>
            public ICollection<K> Keys
            {
                get
                {
                    return this._dictionary.Keys;
                }
            }

            /// <summary>
            /// Gets the values.
            /// </summary>
            public ICollection<V> Values
            {
                get
                {
                    return this._dictionary.Values;
                }
            }

            #endregion

            #region Public Indexers

            /// <summary>
            /// The this.
            /// </summary>
            /// <param name="key">
            /// The key.
            /// </param>
            /// <returns>
            /// The V.
            /// </returns>
            public V this[K key]
            {
                get
                {
                    return this._dictionary[key];
                }

                set
                {
                    this._dictionary[key] = value;
                    this.InvokeMapChanged(CollectionChange.ItemChanged, key);
                }
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// The add.
            /// </summary>
            /// <param name="key">
            /// The key.
            /// </param>
            /// <param name="value">
            /// The value.
            /// </param>
            public void Add(K key, V value)
            {
                this._dictionary.Add(key, value);
                this.InvokeMapChanged(CollectionChange.ItemInserted, key);
            }

            /// <summary>
            /// The add.
            /// </summary>
            /// <param name="item">
            /// The item.
            /// </param>
            public void Add(KeyValuePair<K, V> item)
            {
                this.Add(item.Key, item.Value);
            }

            /// <summary>
            /// The clear.
            /// </summary>
            public void Clear()
            {
                K[] priorKeys = this._dictionary.Keys.ToArray();
                this._dictionary.Clear();
                foreach (K key in priorKeys)
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
                }
            }

            /// <summary>
            /// The contains.
            /// </summary>
            /// <param name="item">
            /// The item.
            /// </param>
            /// <returns>
            /// The System.Boolean.
            /// </returns>
            public bool Contains(KeyValuePair<K, V> item)
            {
                return this._dictionary.Contains(item);
            }

            /// <summary>
            /// The contains key.
            /// </summary>
            /// <param name="key">
            /// The key.
            /// </param>
            /// <returns>
            /// The System.Boolean.
            /// </returns>
            public bool ContainsKey(K key)
            {
                return this._dictionary.ContainsKey(key);
            }

            /// <summary>
            /// The copy to.
            /// </summary>
            /// <param name="array">
            /// The array.
            /// </param>
            /// <param name="arrayIndex">
            /// The array index.
            /// </param>
            public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
            {
                int arraySize = array.Length;
                foreach (var pair in this._dictionary)
                {
                    if (arrayIndex >= arraySize)
                    {
                        break;
                    }

                    array[arrayIndex++] = pair;
                }
            }

            /// <summary>
            /// The get enumerator.
            /// </summary>
            /// <returns>
            /// The System.Collections.Generic.IEnumerator`1[T -&gt; System.Collections.Generic.KeyValuePair`2[TKey -&gt; K, TValue -&gt; V]].
            /// </returns>
            public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
            {
                return this._dictionary.GetEnumerator();
            }

            /// <summary>
            /// The remove.
            /// </summary>
            /// <param name="key">
            /// The key.
            /// </param>
            /// <returns>
            /// The System.Boolean.
            /// </returns>
            public bool Remove(K key)
            {
                if (this._dictionary.Remove(key))
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// The remove.
            /// </summary>
            /// <param name="item">
            /// The item.
            /// </param>
            /// <returns>
            /// The System.Boolean.
            /// </returns>
            public bool Remove(KeyValuePair<K, V> item)
            {
                V currentValue;
                if (this._dictionary.TryGetValue(item.Key, out currentValue) && Equals(item.Value, currentValue)
                    && this._dictionary.Remove(item.Key))
                {
                    this.InvokeMapChanged(CollectionChange.ItemRemoved, item.Key);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// The try get value.
            /// </summary>
            /// <param name="key">
            /// The key.
            /// </param>
            /// <param name="value">
            /// The value.
            /// </param>
            /// <returns>
            /// The System.Boolean.
            /// </returns>
            public bool TryGetValue(K key, out V value)
            {
                return this._dictionary.TryGetValue(key, out value);
            }

            #endregion

            #region Explicit Interface Methods

            /// <summary>
            /// The get enumerator.
            /// </summary>
            /// <returns>
            /// The System.Collections.IEnumerator.
            /// </returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this._dictionary.GetEnumerator();
            }

            #endregion

            #region Methods

            /// <summary>
            /// The invoke map changed.
            /// </summary>
            /// <param name="change">
            /// The change.
            /// </param>
            /// <param name="key">
            /// The key.
            /// </param>
            private void InvokeMapChanged(CollectionChange change, K key)
            {
                MapChangedEventHandler<K, V> eventHandler = this.MapChanged;
                if (eventHandler != null)
                {
                    eventHandler(this, new ObservableDictionaryChangedEventArgs(change, key));
                }
            }

            #endregion

            /// <summary>
            /// The observable dictionary changed event args.
            /// </summary>
            private class ObservableDictionaryChangedEventArgs : IMapChangedEventArgs<K>
            {
                #region Constructors and Destructors

                /// <summary>
                /// Initializes a new instance of the <see cref="ObservableDictionaryChangedEventArgs"/> class.
                /// </summary>
                /// <param name="change">
                /// The change.
                /// </param>
                /// <param name="key">
                /// The key.
                /// </param>
                public ObservableDictionaryChangedEventArgs(CollectionChange change, K key)
                {
                    this.CollectionChange = change;
                    this.Key = key;
                }

                #endregion

                #region Public Properties

                /// <summary>
                /// Gets the collection change.
                /// </summary>
                public CollectionChange CollectionChange { get; private set; }

                /// <summary>
                /// Gets the key.
                /// </summary>
                public K Key { get; private set; }

                #endregion
            }
        }
    }
}