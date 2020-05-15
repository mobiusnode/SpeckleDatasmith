// Copyright Epic Games, Inc. All Rights Reserved.

//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (http://www.swig.org).
// Version 3.0.12
//
// Do not make changes to this file unless you know what you are doing--modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------


public class FDatasmithFacadeMaterial : FDatasmithFacadeElement {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;

  internal FDatasmithFacadeMaterial(global::System.IntPtr cPtr, bool cMemoryOwn) : base(DatasmithFacadeCSharpPINVOKE.FDatasmithFacadeMaterial_SWIGUpcast(cPtr), cMemoryOwn) {
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(FDatasmithFacadeMaterial obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~FDatasmithFacadeMaterial() {
    Dispose();
  }

  public override void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          DatasmithFacadeCSharpPINVOKE.delete_FDatasmithFacadeMaterial(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
      base.Dispose();
    }
  }

  public FDatasmithFacadeMaterial(string InElementName, string InElementLabel) : this(DatasmithFacadeCSharpPINVOKE.new_FDatasmithFacadeMaterial(InElementName, InElementLabel), true) {
  }

  public void SetMasterMaterialType(FDatasmithFacadeMaterial.EMasterMaterialType InMasterMaterialType) {
    DatasmithFacadeCSharpPINVOKE.FDatasmithFacadeMaterial_SetMasterMaterialType(swigCPtr, (int)InMasterMaterialType);
  }

  public void AddColor(string InPropertyName, byte InR, byte InG, byte InB, byte InA) {
    DatasmithFacadeCSharpPINVOKE.FDatasmithFacadeMaterial_AddColor__SWIG_0(swigCPtr, InPropertyName, InR, InG, InB, InA);
  }

  public void AddColor(string InPropertyName, float InR, float InG, float InB, float InA) {
    DatasmithFacadeCSharpPINVOKE.FDatasmithFacadeMaterial_AddColor__SWIG_1(swigCPtr, InPropertyName, InR, InG, InB, InA);
  }

  public void AddTexture(string InPropertyName, string InTextureFilePath, FDatasmithFacadeMaterial.ETextureMode InTextureMode) {
    DatasmithFacadeCSharpPINVOKE.FDatasmithFacadeMaterial_AddTexture__SWIG_0(swigCPtr, InPropertyName, InTextureFilePath, (int)InTextureMode);
  }

  public void AddTexture(string InPropertyName, string InTextureFilePath) {
    DatasmithFacadeCSharpPINVOKE.FDatasmithFacadeMaterial_AddTexture__SWIG_1(swigCPtr, InPropertyName, InTextureFilePath);
  }

  public void AddString(string InPropertyName, string InPropertyValue) {
    DatasmithFacadeCSharpPINVOKE.FDatasmithFacadeMaterial_AddString(swigCPtr, InPropertyName, InPropertyValue);
  }

  public void AddFloat(string InPropertyName, float InPropertyValue) {
    DatasmithFacadeCSharpPINVOKE.FDatasmithFacadeMaterial_AddFloat(swigCPtr, InPropertyName, InPropertyValue);
  }

  public void AddBoolean(string InPropertyName, bool bInPropertyValue) {
    DatasmithFacadeCSharpPINVOKE.FDatasmithFacadeMaterial_AddBoolean(swigCPtr, InPropertyName, bInPropertyValue);
  }

  public enum EMasterMaterialType {
    Opaque,
    Transparent,
    CutOut
  }

  public enum ETextureMode {
    Diffuse,
    Specular,
    Normal,
    NormalGreenInv,
    Displace,
    Other,
    Bump
  }

}
