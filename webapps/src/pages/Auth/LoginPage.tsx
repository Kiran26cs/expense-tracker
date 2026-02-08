import { useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { GoogleLogin } from '@react-oauth/google';
import { useAuth } from '@/hooks/useAuth';
import { authApi } from '@/services/auth.api';
import { Input } from '@/components/Input/Input';
import { Button } from '@/components/Button/Button';
import { isValidEmail, isValidPhone } from '@/utils/helpers';
import styles from './Auth.module.css';

export const LoginPage = () => {
  const [emailOrPhone, setEmailOrPhone] = useState('');
  const [otp, setOtp] = useState('');
  const [step, setStep] = useState<'input' | 'otp'>('input');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [resendTimer, setResendTimer] = useState(0);
  const { login, googleLogin } = useAuth();
  const navigate = useNavigate();
  const googleButtonRef = useRef<HTMLDivElement>(null);

  const handleGoogleSuccess = async (credentialResponse: any) => {
    if (!credentialResponse.credential) {
      setError('Google login failed - no credential received');
      return;
    }
    setLoading(true);
    setError('');
    try {
      await googleLogin(credentialResponse.credential);
      navigate('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Google login failed');
    } finally {
      setLoading(false);
    }
  };

  const validateInput = () => {
    if (!emailOrPhone.trim()) {
      setError('Please enter your email or phone number');
      return false;
    }
    if (!isValidEmail(emailOrPhone) && !isValidPhone(emailOrPhone)) {
      setError('Please enter a valid email or phone number');
      return false;
    }
    return true;
  };

  const handleRequestOTP = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!validateInput()) return;

    setLoading(true);
    try {
      const email = emailOrPhone.includes('@') ? emailOrPhone : undefined;
      const phone = emailOrPhone.includes('@') ? undefined : emailOrPhone;
      const response = await authApi.requestOTP(email, phone);
      if (response.success) {
        setStep('otp');
        setResendTimer(60);
        const timer = setInterval(() => {
          setResendTimer((prev) => {
            if (prev <= 1) {
              clearInterval(timer);
              return 0;
            }
            return prev - 1;
          });
        }, 1000);
      } else {
        setError(response.error || 'Failed to send OTP');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to send OTP');
    } finally {
      setLoading(false);
    }
  };

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (!otp.trim()) {
      setError('Please enter the OTP');
      return;
    }

    setLoading(true);
    try {
      // Verify OTP first
      const email = emailOrPhone.includes('@') ? emailOrPhone : undefined;
      const phone = emailOrPhone.includes('@') ? undefined : emailOrPhone;
      const verifyResponse = await authApi.verifyOTP(email, phone, otp);
      if (!verifyResponse.success) {
        setError('Invalid verification code');
        return;
      }

      // Complete login with verified OTP
      await login(emailOrPhone, otp);
      navigate('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed');
    } finally {
      setLoading(false);
    }
  };

  const handleResendOTP = async () => {
    setError('');
    setLoading(true);
    try {
      const email = emailOrPhone.includes('@') ? emailOrPhone : undefined;
      const phone = emailOrPhone.includes('@') ? undefined : emailOrPhone;
      await authApi.requestOTP(email, phone);
      setResendTimer(60);
      const timer = setInterval(() => {
        setResendTimer((prev) => {
          if (prev <= 1) {
            clearInterval(timer);
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
    } catch (err) {
      setError('Failed to resend OTP');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className={styles['auth-container']}>
      <div className={styles['auth-wrapper']}>
        {/* Left Side - Form Section */}
        <div className={styles['auth-card']}>
          <div className={styles['auth-form-container']}>
            <div className={styles['auth-header']}>
              <div className={styles['auth-logo']}>E</div>
              <h1 className={styles['auth-title']}>Sign In</h1>
              <p className={styles['auth-subtitle']}>or use your email account</p>
            </div>

            {step === 'input' ? (
              <>
                {/* Social Icons */}
                <div className={styles['social-icons']}>
                  <div 
                    className={`${styles['social-icon']} ${styles['google']}`}
                    onClick={() => {
                      // Trigger Google login by clicking the hidden button
                      const btn = googleButtonRef.current?.querySelector('div[role="button"]') as HTMLElement;
                      btn?.click();
                    }}
                    title="Sign in with Google"
                  >
                    <i className="fab fa-google"></i>
                  </div>
                  <div 
                    className={`${styles['social-icon']} ${styles['facebook']} ${styles['disabled']}`}
                    title="Facebook login (coming soon)"
                  >
                    <i className="fab fa-facebook-f"></i>
                  </div>
                  <div 
                    className={`${styles['social-icon']} ${styles['linkedin']} ${styles['disabled']}`}
                    title="LinkedIn login (coming soon)"
                  >
                    <i className="fab fa-linkedin-in"></i>
                  </div>
                </div>

                {/* Hidden Google Login Button */}
                <div ref={googleButtonRef} style={{ position: 'absolute', left: '-9999px' }}>
                  <GoogleLogin
                    onSuccess={handleGoogleSuccess}
                    onError={() => setError('Google login failed')}
                    size="large"
                  />
                </div>

                <div className={styles['auth-divider']}>or</div>

                <form className={styles['auth-form']} onSubmit={handleRequestOTP}>
                  <Input
                    label="Email"
                    type="text"
                    value={emailOrPhone}
                    onChange={(e) => setEmailOrPhone(e.target.value)}
                    placeholder="Enter your email"
                    error={error}
                    required
                  />

                  <Button type="submit" fullWidth loading={loading}>
                    Sign In
                  </Button>
                </form>

                <div className={styles['auth-footer']}>
                  Don't have an account?{' '}
                  <a href="/signup" className={styles['auth-link']}>
                    Sign up
                  </a>
                </div>
              </>
            ) : (
              <form className={styles['auth-form']} onSubmit={handleLogin}>
                <div className={styles['otp-container']}>
                  <p className={styles['otp-info']}>
                    We've sent a verification code to <strong>{emailOrPhone}</strong>
                  </p>

                  <Input
                    label="Verification Code"
                    type="text"
                    value={otp}
                    onChange={(e) => setOtp(e.target.value)}
                    placeholder="Enter 6-digit code"
                    maxLength={6}
                    error={error}
                    required
                  />

                  <button
                    type="button"
                    className={styles['otp-resend']}
                    onClick={handleResendOTP}
                    disabled={resendTimer > 0 || loading}
                  >
                    {resendTimer > 0 ? `Resend in ${resendTimer}s` : 'Resend Code'}
                  </button>
                </div>

                <Button type="submit" fullWidth loading={loading}>
                  Sign In
                </Button>

                <Button
                  type="button"
                  variant="ghost"
                  fullWidth
                  onClick={() => {
                    setStep('input');
                    setOtp('');
                    setError('');
                  }}
                >
                  Use Different Email/Phone
                </Button>
              </form>
            )}
          </div>
        </div>

        {/* Right Side - Welcome Section */}
        <div className={styles['auth-left']}>
          <div className={styles['auth-left-content']}>
            <h1>Hello, Friend!</h1>
            <p>Enter your personal details and start your journey with us</p>
            <button 
              className={styles['auth-left-button']}
              onClick={() => navigate('/signup')}
            >
              Sign Up
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
