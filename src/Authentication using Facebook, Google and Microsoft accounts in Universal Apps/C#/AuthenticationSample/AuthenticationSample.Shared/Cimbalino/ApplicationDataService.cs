﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApplicationDataService.cs" company="saramgsilva">
//   Copyright (c) 2013 saramgsilva. All rights reserved. 
// </copyright>
// <summary>
//   The ApplicationDataService interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using Windows.Foundation.Collections;

// ReSharper disable once CheckNamespace
namespace Cimbalino.Toolkit.Services
{
    /// <summary>
    /// The ApplicationDataService interface.
    /// </summary>
    public interface IApplicationDataService
    {
        /// <summary>
        /// Gets the local settings.
        /// </summary>
        /// <value>
        /// The local settings.
        /// </value>
        IPropertySet LocalSettings { get; }

        /// <summary>
        /// Gets the roaming settings.
        /// </summary>
        /// <value>
        /// The roaming settings.
        /// </value>
        IPropertySet RoamingSettings { get; }
    }

    /// <summary>
    /// The application data service.
    /// </summary>
    public class ApplicationDataService : IApplicationDataService
    {
        /// <summary>
        /// Gets the local settings.
        /// </summary>
        /// <value>
        /// The local settings.
        /// </value>
        public IPropertySet LocalSettings
        {
            get { return Windows.Storage.ApplicationData.Current.LocalSettings.Values; }
        }

        /// <summary>
        /// Gets the roaming settings.
        /// </summary>
        /// <value>
        /// The roaming settings.
        /// </value>
        public IPropertySet RoamingSettings
        {
            get { return Windows.Storage.ApplicationData.Current.RoamingSettings.Values; }
        }
    }
}
