using System;
using System.Reflection;
using System.Windows.Forms;
using log4net;
using SlimDX;
using SlimDX.Direct3D9;

namespace RTS {
    public enum ShaderType {
        VertexShader,
        PixelShader,
    }
    internal class Shader : DisposableClass {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ShaderType Type { get; set; }
        private Device _device;
        private PixelShader _pixelShader;
        private VertexShader _vertexShader;
        private ConstantTable _constantTable;

        private Shader() {
            _pixelShader = null;
            _vertexShader = null;
            _constantTable = null;
        }




        public Shader(Device device, string shaderFile, ShaderType type) :this() {
            _device = device;
            Type = type;
            if (_device == null) return;

            string errors = null;
            try {
                ShaderBytecode bytecode;
                if (Type == ShaderType.PixelShader) {
                    bytecode = ShaderBytecode.CompileFromFile(shaderFile, null, null, "Main", "ps_2_0", ShaderFlags.Debug, out errors);
                    _pixelShader = new PixelShader(_device, bytecode);
                } else {
                    bytecode = ShaderBytecode.CompileFromFile(shaderFile, null, null, "Main", "vs_2_0", ShaderFlags.Debug, out errors);
                    _vertexShader = new VertexShader(_device, bytecode);
                }
                _constantTable = bytecode.ConstantTable;
            } catch (Exception ex) {
                Log.Error("Exception in " + ex.TargetSite.Name, ex);
                Log.Error(errors);
                Application.Exit();
            }
        }

        public void Begin() {
            if (Type == ShaderType.PixelShader) {
                _device.PixelShader = _pixelShader;
            } else {
                _device.VertexShader = _vertexShader;
            }
        }

        public void End() {
            if (Type == ShaderType.PixelShader) {
                _device.PixelShader = null;
            } else {
                _device.VertexShader = null;
            }
        }

        public EffectHandle GetConstant(string name) {return _constantTable.GetConstant(null, name);}
        public void SetFloat(EffectHandle h, float f) {_constantTable.SetValue(_device, h, f);}
        public void SetVector3(EffectHandle h, Vector3 v) { _constantTable.SetValue(_device, h, v); }
        public void SetVector4(EffectHandle h, Vector4 v) { _constantTable.SetValue(_device, h, v); }
        public void SetMatrix(EffectHandle h, Matrix m) { _constantTable.SetValue(_device, h, m); }
        
        private bool _disposed;
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _pixelShader);
                    Util.ReleaseCom(ref _vertexShader);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}