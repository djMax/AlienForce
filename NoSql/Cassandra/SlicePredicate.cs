/**
 * Autogenerated by Thrift
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Thrift;
using Thrift.Collections;
using Thrift.Protocol;
using Thrift.Transport;
namespace AlienForce.NoSql.Cassandra
{

  [Serializable]
  public partial class SlicePredicate : TBase
  {
    private List<byte[]> column_names;
    private SliceRange slice_range;

    public List<byte[]> Column_names
    {
      get
      {
        return column_names;
      }
      set
      {
        __isset.column_names = true;
        this.column_names = value;
      }
    }

    public SliceRange Slice_range
    {
      get
      {
        return slice_range;
      }
      set
      {
        __isset.slice_range = true;
        this.slice_range = value;
      }
    }


    public Isset __isset;
    [Serializable]
    public struct Isset {
      public bool column_names;
      public bool slice_range;
    }

    public SlicePredicate() {
    }

    public void Read (TProtocol iprot)
    {
      TField field;
      iprot.ReadStructBegin();
      while (true)
      {
        field = iprot.ReadFieldBegin();
        if (field.Type == TType.Stop) { 
          break;
        }
        switch (field.ID)
        {
          case 1:
            if (field.Type == TType.List) {
              {
                this.column_names = new List<byte[]>();
                TList _list8 = iprot.ReadListBegin();
                for( int _i9 = 0; _i9 < _list8.Count; ++_i9)
                {
                  byte[] _elem10 = null;
                  _elem10 = iprot.ReadBinary();
                  this.column_names.Add(_elem10);
                }
                iprot.ReadListEnd();
              }
              this.__isset.column_names = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          case 2:
            if (field.Type == TType.Struct) {
              this.slice_range = new SliceRange();
              this.slice_range.Read(iprot);
              this.__isset.slice_range = true;
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          default: 
            TProtocolUtil.Skip(iprot, field.Type);
            break;
        }
        iprot.ReadFieldEnd();
      }
      iprot.ReadStructEnd();
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("SlicePredicate");
      oprot.WriteStructBegin(struc);
      TField field = new TField();
      if (this.column_names != null && __isset.column_names) {
        field.Name = "column_names";
        field.Type = TType.List;
        field.ID = 1;
        oprot.WriteFieldBegin(field);
        {
          oprot.WriteListBegin(new TList(TType.String, this.column_names.Count));
          foreach (byte[] _iter11 in this.column_names)
          {
            oprot.WriteBinary(_iter11);
            oprot.WriteListEnd();
          }
        }
        oprot.WriteFieldEnd();
      }
      if (this.slice_range != null && __isset.slice_range) {
        field.Name = "slice_range";
        field.Type = TType.Struct;
        field.ID = 2;
        oprot.WriteFieldBegin(field);
        this.slice_range.Write(oprot);
        oprot.WriteFieldEnd();
      }
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("SlicePredicate(");
      sb.Append("column_names: ");
      sb.Append(this.column_names);
      sb.Append(",slice_range: ");
      sb.Append(this.slice_range== null ? "<null>" : this.slice_range.ToString());
      sb.Append(")");
      return sb.ToString();
    }

  }

}
