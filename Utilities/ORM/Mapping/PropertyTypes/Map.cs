﻿/*
Copyright (c) 2012 <a href="http://www.gutgames.com">James Craig</a>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.*/

#region Usings
using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Utilities.ORM.Mapping.BaseClasses;
using Utilities.ORM.Mapping.Interfaces;
using Utilities.ORM.QueryProviders.Interfaces;
using Utilities.Reflection.ExtensionMethods;
using Utilities.SQL.Interfaces;
using Utilities.SQL.MicroORM;
using Utilities.SQL;
using System.Collections.Generic;
using Utilities.DataTypes.ExtensionMethods;
#endregion

namespace Utilities.ORM.Mapping.PropertyTypes
{
    /// <summary>
    /// Map class
    /// </summary>
    /// <typeparam name="ClassType">Class type</typeparam>
    /// <typeparam name="DataType">Data type</typeparam>
    public class Map<ClassType, DataType> : PropertyBase<ClassType, DataType, IMap<ClassType, DataType>>,
        IMap<ClassType, DataType>
        where ClassType : class,new()
        where DataType : class,new()
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Expression">Expression pointing to the Map</param>
        /// <param name="Mapping">Mapping the StringID is added to</param>
        public Map(Expression<Func<ClassType, DataType>> Expression, IMapping Mapping)
            : base(Expression, Mapping)
        {
            if (typeof(DataType).IsIEnumerable())
                throw new ArgumentException("Expression is an IEnumerable, use ManyToOne or ManyToMany instead");
            SetDefaultValue(() => default(DataType));
            SetFieldName(typeof(DataType).Name + "_" + Name + "_ID");
        }

        #endregion

        #region Functions

        public override void SetupLoadCommands()
        {
            if (this.CommandToLoad != null)
                return;
            IMapping ForeignMapping = Mapping.Manager.Mappings[typeof(DataType)].First(x => x.DatabaseConfigType == Mapping.DatabaseConfigType);
            if (ForeignMapping.TableName == Mapping.TableName)
            {
                LoadUsingCommand(@"SELECT " + ForeignMapping.TableName + @"2.*
                                FROM " + ForeignMapping.TableName + @" AS "+ForeignMapping.TableName+@"2
                                INNER JOIN " + Mapping.TableName + " ON " + Mapping.TableName + "." + FieldName + "=" + ForeignMapping.TableName + "2." + ForeignMapping.IDProperty.FieldName + @"
                                WHERE " + Mapping.TableName + "." + Mapping.IDProperty.FieldName + "=@ID", CommandType.Text);
            }
            else
            {
                LoadUsingCommand(@"SELECT " + ForeignMapping.TableName + @".*
                                FROM " + ForeignMapping.TableName + @"
                                INNER JOIN " + Mapping.TableName + " ON " + Mapping.TableName + "." + FieldName + "=" + ForeignMapping.TableName + "." + ForeignMapping.IDProperty.FieldName + @"
                                WHERE " + Mapping.TableName + "." + Mapping.IDProperty.FieldName + "=@ID", CommandType.Text);
            }
        }

        public override IEnumerable<Command> JoinsDelete(ClassType Object, MicroORM MicroORM)
        {
            return new List<Command>();
        }

        public override IEnumerable<Command> JoinsSave(ClassType Object, MicroORM MicroORM)
        {
            return new List<Command>();
        }

        public override IEnumerable<Command> CascadeJoinsDelete(ClassType Object, MicroORM MicroORM)
        {
            if (Object == null)
                return new List<Command>();
            DataType Item = CompiledExpression(Object);
            if (Item == null)
                return new List<Command>();
            List<Command> Commands = new List<Command>();
            foreach (IProperty Property in Mapping.Manager.Mappings[typeof(DataType)].First(x => x.DatabaseConfigType == Mapping.DatabaseConfigType).Properties)
            {
                if (!Property.Cascade &&
                        (Property is IManyToMany
                            || Property is IManyToOne
                            || Property is IIEnumerableManyToOne
                            || Property is IListManyToMany
                            || Property is IListManyToOne))
                {
                    Commands.AddIfUnique(((IProperty<DataType>)Property).JoinsDelete(Item, MicroORM));
                }
                if (Property.Cascade)
                {
                    Commands.AddIfUnique(((IProperty<DataType>)Property).CascadeJoinsDelete(Item, MicroORM));
                }
            }
            Commands.AddIfUnique(JoinsDelete(Object, MicroORM));
            return Commands;
        }

        public override IEnumerable<Command> CascadeJoinsSave(ClassType Object, MicroORM MicroORM)
        {
            if (Object == null)
                return new List<Command>();
            DataType Item = CompiledExpression(Object);
            if (Item == null)
                return new List<Command>();
            List<Command> Commands = new List<Command>();
            foreach (IProperty Property in Mapping.Manager.Mappings[typeof(DataType)].First(x => x.DatabaseConfigType == Mapping.DatabaseConfigType).Properties)
            {
                if (!Property.Cascade &&
                        (Property is IManyToMany
                            || Property is IManyToOne
                            || Property is IIEnumerableManyToOne
                            || Property is IListManyToMany
                            || Property is IListManyToOne))
                {
                    Commands.AddIfUnique(((IProperty<DataType>)Property).JoinsSave(Item, MicroORM));
                }
                if (Property.Cascade)
                {
                    Commands.AddIfUnique(((IProperty<DataType>)Property).CascadeJoinsSave(Item, MicroORM));
                }
            }
            Commands.AddIfUnique(JoinsDelete(Object, MicroORM));
            return Commands;
        }

        public override void CascadeDelete(ClassType Object, MicroORM MicroORM)
        {
            if (Object == null)
                return;
            DataType Item = CompiledExpression(Object);
            if (Item == null)
                return;
            foreach (IProperty Property in Mapping.Manager.Mappings[typeof(DataType)].First(x => x.DatabaseConfigType == Mapping.DatabaseConfigType).Properties)
            {
                if (Property.Cascade)
                    ((IProperty<DataType>)Property).CascadeDelete(Item, MicroORM);
            }
            ((IProperty<DataType>)Mapping.Manager.Mappings[typeof(DataType)].First(x => x.DatabaseConfigType == Mapping.DatabaseConfigType).IDProperty).CascadeDelete(Item, MicroORM);
        }

        public override void CascadeSave(ClassType Object, MicroORM MicroORM)
        {
            if (Object == null)
                return;
            DataType Item = CompiledExpression(Object);
            if (Item == null)
                return;
            foreach (IProperty Property in Mapping.Manager.Mappings[typeof(DataType)].First(x => x.DatabaseConfigType == Mapping.DatabaseConfigType).Properties)
            {
                if (Property.Cascade)
                    ((IProperty<DataType>)Property).CascadeSave(Item, MicroORM);
            }
            ((IProperty<DataType>)Mapping.Manager.Mappings[typeof(DataType)].First(x => x.DatabaseConfigType == Mapping.DatabaseConfigType).IDProperty).CascadeSave(Item, MicroORM);
        }

        public override IParameter GetAsParameter(ClassType Object)
        {
            if (Object == null)
                return null;
            DataType Item = CompiledExpression(Object);
            if (Item == null)
                return null;
            IParameter Parameter = ((IProperty<DataType>)Mapping.Manager.Mappings[typeof(DataType)].First(x => x.DatabaseConfigType == Mapping.DatabaseConfigType).IDProperty).GetAsParameter(Item);
            Parameter.ID = FieldName;
            return Parameter;
        }

        public override object GetAsObject(ClassType Object)
        {
            if (Object == null)
                return null;
            DataType Item = CompiledExpression(Object);
            if (Item == null)
                return null;
            return ((IProperty<DataType>)Mapping.Manager.Mappings[typeof(DataType)].First(x => x.DatabaseConfigType == Mapping.DatabaseConfigType).IDProperty).GetAsObject(Item);
        }

        public override IMap<ClassType, DataType> LoadUsingCommand(string Command, System.Data.CommandType CommandType)
        {
            this.CommandToLoad = new Command(Command, CommandType);
            return (IMap<ClassType, DataType>)this;
        }

        public override void AddToQueryProvider(IDatabase Database, Mapping<ClassType> Mapping)
        {
        }

        public override IMap<ClassType, DataType> SetDefaultValue(Func<DataType> DefaultValue)
        {
            this.DefaultValue = DefaultValue;
            return (IMap<ClassType, DataType>)this;
        }

        public override IMap<ClassType, DataType> DoNotAllowNullValues()
        {
            this.NotNull = true;
            return (IMap<ClassType, DataType>)this;
        }

        public override IMap<ClassType, DataType> ThisShouldBeUnique()
        {
            this.Unique = true;
            return (IMap<ClassType, DataType>)this;
        }

        public override IMap<ClassType, DataType> TurnOnIndexing()
        {
            this.Index = true;
            return (IMap<ClassType, DataType>)this;
        }

        public override IMap<ClassType, DataType> TurnOnAutoIncrement()
        {
            this.AutoIncrement = true;
            return (IMap<ClassType, DataType>)this;
        }

        public override IMap<ClassType, DataType> SetFieldName(string FieldName)
        {
            this.FieldName = FieldName;
            return (IMap<ClassType, DataType>)this;
        }


        public override IMap<ClassType, DataType> SetTableName(string TableName)
        {
            this.TableName = TableName;
            return (IMap<ClassType, DataType>)this;
        }

        public override IMap<ClassType, DataType> TurnOnCascade()
        {
            this.Cascade = true;
            return (IMap<ClassType, DataType>)this;
        }

        public override IMap<ClassType, DataType> SetMaxLength(int MaxLength)
        {
            this.MaxLength = MaxLength;
            return (IMap<ClassType, DataType>)this;
        }

        #endregion
    }
}