import React, { useEffect, useState } from 'react'

const Dashboard = () => {

    return (
        <div className='w-full h-100vh flex flex-col items-center justify-center space-y-3'>
            <div>Плейсхолдер для уведомлений</div>
            <div>
                Выбор периода
            </div>
            <div className='flex'>
                <div className='p-6 mx-5 border'>
                    Суммарные расходы за период: 25000 ₽
                </div>
                <div className='p-6 mx-5 border'>
                Суммарные доходы за период: 50000 ₽
                </div>
            </div>
            <div className='p-10 border'>
                Баланс за период: 25000 ₽
            </div>
            <div>
                График/Диаграмма распределения расходов по категориям за период
            </div>
            <div>
                Список последних 5 транзакций за период
            </div>
        </div>
    )
}

export default Dashboard