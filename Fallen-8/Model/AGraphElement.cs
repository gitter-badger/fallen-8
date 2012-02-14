// 
// AGraphElement.cs
//  
// Author:
//       Henning Rauch <Henning@RauchEntwicklung.biz>
// 
// Copyright (c) 2012 Henning Rauch
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using Fallen8.API.Helper;
using Fallen8.API.Error;

namespace Fallen8.API.Model
{
    /// <summary>
    /// A graph element.
    /// </summary>
    public abstract class AGraphElement : AThreadSafeElement
    {
        #region Data
        
        /// <summary>
        /// The identifier of this graph element.
        /// </summary>
        public readonly Int32 Id;
        
        /// <summary>
        /// The creation date.
        /// </summary>
        public readonly Int64 CreationDate;
        
        /// <summary>
        /// The modification date.
        /// </summary>
        public Int64 ModificationDate;
        
        /// <summary>
        /// The properties.
        /// </summary>
        private List<PropertyContainer> _properties;
  
        #endregion
        
        #region constructor
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AGraphElement"/> class.
        /// </summary>
        /// <param name='id'>
        /// Identifier.
        /// </param>
        /// <param name='creationDate'>
        /// Creation date.
        /// </param>
        /// <param name='properties'>
        /// Properties.
        /// </param>
        protected AGraphElement(Int32 id, Int64 creationDate, List<PropertyContainer> properties)
        {
            Id = id;
            CreationDate = creationDate;
            ModificationDate = creationDate;
            _properties = properties;
        }
        
        #endregion
        
        #region public methods

        /// <summary>
        /// Gets all properties.
        /// </summary>
        /// <returns>
        /// All properties.
        /// </returns>
        public IEnumerable<PropertyContainer> GetAllProperties()
        {
            if (ReadResource())
            {
                if (_properties != null)
                {
                    foreach(var aProperty in _properties)
                    {
                        if (aProperty.Value != null) 
                        {
                            yield return aProperty;    
                        }
                    }
                }
                
                FinishReadResource();
                
                yield break;
            }

            throw new CollisionException();
        }

        /// <summary>
        /// Tries the get property.
        /// </summary>
        /// <typeparam name="TProperty">Type of the property</typeparam>
        /// <param name="result">Result.</param>
        /// <param name="propertyId">Property identifier.</param>
        /// <returns><c>true</c> if something was found; otherwise, <c>false</c>.</returns>
        public Boolean TryGetProperty<TProperty>(out TProperty result, Int32 propertyId)
        {
            if (ReadResource())
            {
                foreach (var aPropertyContainer in _properties) 
                {
                    if (aPropertyContainer.Value != null && aPropertyContainer.PropertyId == propertyId) 
                    {
                        result = (TProperty) aPropertyContainer.Value;
                        
                        FinishReadResource();
                        
                        return true;
                    }
                }
                
                result = default(TProperty);
                
                FinishReadResource();

                return false;
            }

            throw new CollisionException();
        }
 
        #endregion

        #region internal methods

        /// <summary>
        /// Trims the graph element
        /// </summary>
        internal abstract void Trim();

        /// <summary>
        /// Tries to add a property.
        /// </summary>
        /// <returns>
        /// <c>true</c> if it was an update; otherwise, <c>false</c>.
        /// </returns>
        /// <param name='propertyId'>
        /// If set to <c>true</c> property identifier.
        /// </param>
        /// <param name='property'>
        /// If set to <c>true</c> property.
        /// </param>
        /// <exception cref='CollisionException'>
        /// Is thrown when the collision exception.
        /// </exception>
        internal bool TryAddProperty(Int32 propertyId, object property)
        {
            if (WriteResource())
            {
                var foundProperty = false;
                var idx = 0;

                if (_properties != null)
                {
                    for (var i = 0; i < _properties.Count; i++)
                    {
                        if (_properties[i].PropertyId == propertyId)
                        {
                            foundProperty = true;
                            idx = i;
                            break;
                        }
                    }

                    if (!foundProperty)
                    {
                        _properties.Add(new PropertyContainer { PropertyId = propertyId, Value = property });
                    }
                    else
                    {
                        _properties[idx] = new PropertyContainer { PropertyId = propertyId, Value = property };
                    }
                }
                else
                {
                    _properties = new List<PropertyContainer> { new PropertyContainer { PropertyId = propertyId, Value = property } };
                }

                //set the modificationdate
                ModificationDate = DateTime.Now.ToBinary();

                FinishWriteResource();

                return foundProperty;
            }

            throw new CollisionException();
        }

        /// <summary>
        /// Tries to remove a property.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the property was removed; otherwise, <c>false</c> if there was no such property.
        /// </returns>
        /// <param name='propertyId'>
        /// If set to <c>true</c> property identifier.
        /// </param>
        /// <exception cref='CollisionException'>
        /// Is thrown when the collision exception.
        /// </exception>
        internal bool TryRemoveProperty(Int32 propertyId)
        {
            if (WriteResource())
            {
                var removedSomething = false;

                if (_properties != null)
                {
                    var toBeRemovedIdx = 0;

                    for (var i = 0; i < _properties.Count; i++)
                    {
                        if (_properties[i].PropertyId == propertyId)
                        {
                            toBeRemovedIdx = i;
                            removedSomething = true;
                            break;
                        }
                    }

                    if (removedSomething)
                    {
                        _properties.RemoveAt(toBeRemovedIdx);

                        //set the modificationdate
                        ModificationDate = DateTime.Now.ToBinary();
                    }
                }
                FinishWriteResource();

                return removedSomething;
            }

            throw new CollisionException();

        }

        #endregion

        #region protected members

        /// <summary>
        /// Trims the properties for size
        /// </summary>
        protected void TrimProperties()
        {
            if (WriteResource())
            {
                if (_properties != null)
                {
                    _properties.TrimExcess();                    
                }

                FinishWriteResource();

                return;
            }

            throw new CollisionException();
        }

        #endregion
    }
}

