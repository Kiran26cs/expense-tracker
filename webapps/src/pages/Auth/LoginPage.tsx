import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
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
  const { login } = useAuth();
  const navigate = useNavigate();

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
      <div className={styles['auth-card']}>
        <div className={styles['auth-header']}>
          <div className={styles['auth-logo']}>E</div>
          <h1 className={styles['auth-title']}>Welcome Back</h1>
          <p className={styles['auth-subtitle']}>Sign in to manage your expenses</p>
        </div>

        {step === 'input' ? (
          <form className={styles['auth-form']} onSubmit={handleRequestOTP}>
            <Input
              label="Email or Phone Number"
              type="text"
              value={emailOrPhone}
              onChange={(e) => setEmailOrPhone(e.target.value)}
              placeholder="Enter your email or phone"
              error={error}
              required
            />

            <Button type="submit" fullWidth loading={loading}>
              Continue
            </Button>
          </form>
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

        <div className={styles['auth-footer']}>
          Don't have an account?{' '}
          <a href="/signup" className={styles['auth-link']}>
            Sign up
          </a>
        </div>
      </div>
    </div>
  );
};
